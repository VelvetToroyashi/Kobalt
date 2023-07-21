using Kobalt.Core;
using System.Reflection;
using System.Text;
using Kobalt.Core.Handlers;
using Kobalt.Core.Services.Discord;
using Kobalt.Infrastructure;
using Kobalt.Infrastructure.Extensions;
using Kobalt.Infrastructure.Services;
using Kobalt.Infrastructure.Types;
using Kobalt.Reminders.API.Extensions;
using Kobalt.Shared.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Polly;
using Remora.Commands.Extensions;
using Remora.Discord.API.Abstractions.Gateway.Commands;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Gateway.Commands;
using Remora.Discord.API.Objects;
using Remora.Discord.Caching.Extensions;
using Remora.Discord.Caching.Redis.Extensions;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Responders;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Extensions;
using Remora.Discord.Interactivity.Extensions;
using RemoraHTTPInteractions.Extensions;
using RemoraHTTPInteractions.Services;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
       .AddEnvironmentVariables()
       .AddUserSecrets(Assembly.GetExecutingAssembly(), true);

builder.Services.AddSerilogLogging();

ConfigureKobaltBotServices(builder.Configuration, builder.Services);

var initResult = PluginHelper.LoadPlugins(builder.Services);

 if (!initResult.IsSuccess)
 {
     Log.Fatal("Failed to load plugins: {Error}", initResult.Error);
     return;
 }

builder.WebHost.ConfigureKestrel(c => c.ListenLocalhost(builder.Configuration.GetKobaltConfig().ApiPort));
builder.Host.AddStartupTaskSupport();

// Add services to the container.
builder.Services.AddRazorComponents();

var app = builder.Build();
initResult = await PluginHelper.InitializePluginsAsync(app.Services);

if (!initResult.IsSuccess)
{
    Log.Fatal("Failed to initialize plugins: {Error}", initResult.Error);
    return;
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.MapRazorComponents<App>();

app.MapPost("/interaction", async (HttpContext ctx, WebhookInteractionHelper handler, IOptions<KobaltConfig> config) =>
    {
        var hasHeaders = DiscordHeaders.TryExtractHeaders(ctx.Request.Headers, out var timestamp, out var signature);
        var body = await new StreamReader(ctx.Request.Body).ReadToEndAsync();

        if (!hasHeaders || !DiscordHeaders.VerifySignature(body, timestamp!, signature!, config.Value.Discord.PublicKey!))
        {
            ctx.Response.StatusCode = 401;
            Console.WriteLine("Interaction Validation Failed.");
            return;
        }

        var result = await handler.HandleInteractionAsync(body);
        if (!result.IsDefined(out var content))
        {
            ctx.Response.StatusCode = 500;
            Console.WriteLine($"Interaction Handling Failed. Result: {result.Error}");
            return;
        }

        if (!content.Item2.HasValue)
        {
            ctx.Response.Headers.ContentType = "application/json";
            await ctx.Response.WriteAsync(content.Item1);
        }
        else
        {
            var ret = new MultipartResult().AddPayload(new MemoryStream(Encoding.UTF8.GetBytes(result.Entity.Item1)));

            foreach ((var key, var value) in result.Entity.Item2.Value)
                ret.Add(key, value);

            await ret.ExecuteResultAsync(new ActionContext(ctx, ctx.GetRouteData(), new()));
        }
    }
);

app.Run();

void ConfigureRedis(IConfiguration config, IServiceCollection services)
{
    var connString = config.GetConnectionString("Redis");
    services.AddDiscordRedisCaching(s => s.Configuration = connString);
    
    var multiplexer = ConnectionMultiplexer.Connect(connString);
    services.AddSingleton<IConnectionMultiplexer>(multiplexer);
}

void ConfigureKobaltBotServices(IConfiguration hostConfig, IServiceCollection services)
{
    var config = hostConfig.Get<KobaltConfig>()!;
    services.AddSingleton(Options.Create(config));

    var token = config.Discord.Token;

    services.AddOffsetServices();
    services.AddDiscordGateway(_ => token);
    services.AddInteractivity();
    services.AddHTTPInteractionAPIs();
    services.AddDiscordCommands(true);
    services.AddPostExecutionEvent<PostExecutionHandler>();
    services.AddHostedService<KobaltDiscordGatewayService>();
    services.Configure<InteractionResponderOptions>(s => s.UseEphemeralResponses = true);

    ConfigureRedis(hostConfig, services);
    
    const string CacheKey = "<>k__SelfUserCacheKey_d270867";
    services.AddStartupTask
    (
        async provider =>
        {
            var cache = provider.GetRequiredService<IMemoryCache>();
            var users = provider.GetRequiredService<IDiscordRestUserAPI>();

            var result = await users.GetCurrentUserAsync();

            if (!result.IsSuccess)
            {
                Log.Fatal("Failed to get current user: {Error}", result.Error);
                throw new InvalidOperationException("Failed to get current user");
            }

            cache.Set(CacheKey, result.Entity);
        }
    );

    services.AddSingleton<IUser>(s => s.GetRequiredService<IMemoryCache>().Get<IUser>(CacheKey)!);

    services.AddMemoryCache();
    services.AddDiscordCaching();
    services.AddCondition<EnsureHierarchyCondition>();
    services.AddSingleton<IAsyncPolicy<HttpResponseMessage>>(Policy<HttpResponseMessage>.Handle<HttpRequestException>().WaitAndRetryAsync(5, i => TimeSpan.FromSeconds(Math.Log(i * i) + 1)));

    services.Configure<DiscordGatewayClientOptions>
    (
        options =>
        {
            options.Intents |= GatewayIntents.MessageContents | GatewayIntents.GuildVoiceStates;
            options.Presence = new UpdatePresence
            (
                Status: UserStatus.Idle,
                IsAFK: false,
                Since: null,
                Activities: new IActivity[]
                {
                    new Activity
                    (
                        Name: "Code being written",
                        Type: ActivityType.Watching
                    )
                }
            );
        }
    );
}