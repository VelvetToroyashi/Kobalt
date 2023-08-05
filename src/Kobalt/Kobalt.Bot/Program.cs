using System.Diagnostics;
using System.Reflection;
using System.Text;
using Kobalt.Bot.Autocomplete;
using Kobalt.Bot.Data;
using Kobalt.Bot.Handlers;
using Kobalt.Bot.Services;
using Kobalt.Bot.Services.Discord;
using Kobalt.Infrastructure;
using Kobalt.Infrastructure.Extensions;
using Kobalt.Infrastructure.Services;
using Kobalt.Infrastructure.Services.Booru;
using Kobalt.Infrastructure.Types;
using Kobalt.Shared.Extensions;
using Kobalt.Shared.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Polly;
using Remora.Commands.Extensions;
using Remora.Discord.API.Abstractions.Gateway.Commands;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Gateway.Commands;
using Remora.Discord.Caching.Extensions;
using Remora.Discord.Caching.Redis.Extensions;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Responders;
using Remora.Discord.Extensions.Extensions;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Extensions;
using Remora.Discord.Interactivity.Extensions;
using RemoraHTTPInteractions.Extensions;
using RemoraHTTPInteractions.Services;
using Serilog;
using StackExchange.Redis;
using Activity = Remora.Discord.API.Objects.Activity;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
       .AddEnvironmentVariables()
       .AddUserSecrets(Assembly.GetExecutingAssembly(), true);

builder.Services.AddSerilogLogging();

ConfigureKobaltBotServices(builder.Configuration, builder.Services);

builder.WebHost.ConfigureKestrel(c => c.ListenLocalhost(builder.Configuration.GetKobaltConfig().ApiPort));
builder.Host.AddStartupTaskSupport();

var host = builder.Build();

host.MapPost("/interaction", async (HttpContext ctx, WebhookInteractionHelper handler, IOptions<KobaltConfig> config) =>
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

await host.RunAsync();

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

    services.AddTransient<IChannelLoggerService, ChannelLoggerService>();
    services.AddTransient<ImageOverlayService>();

    services.AddHttpClient("booru");
    services.AddTransient<BooruSearchService>();

    services.AddMediatR(s => s.RegisterServicesFromAssemblyContaining<KobaltContext>());

    services.AddDbContextFactory<KobaltContext>("Kobalt");

    var asm = Assembly.GetExecutingAssembly();

    services.AddRespondersFromAssembly(asm);
    services.AddInteractivityFromAssembly(asm);
    services.AddCommandGroupsFromAssembly(asm, typeFilter: t => !t.IsNested);

    services.AddSingleton<AntiRaidV2Service>();
    services.AddSingleton<ChannelWatcherService>();
    services.AddSingleton<MessagePurgeService>();
    
    services.AddHttpClient
    (
        "Reminders",
        (s, c) =>
        {
            var address = s.GetService<IConfiguration>()!["Kobalt:RemindersApiUrl"] ??
                          throw new KeyNotFoundException("The API url was not configured.");

            c.BaseAddress = new Uri(address);
        }
    );
    
    services.AddAutocompleteProvider<ReminderAutoCompleteProvider>()
            .AddAutocompleteProvider<RoleMenuAutocompleteProvider>();

    services.AddRabbitMQ();
    services.AddSingleton<ReminderAPIService>();

    AddPhishingServices(services);
    AddInfractionServices(services);
    
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

    services.AddStartupTask
    (
        async provider =>
        {
            var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Kobalt");
            logger.LogInformation("Migrating Database");

            var now = Stopwatch.GetTimestamp();
            
            await using var db = provider.GetRequiredService<IDbContextFactory<KobaltContext>>().CreateDbContext();

            await db.Database.MigrateAsync();
            
            logger.LogInformation("Migrated DB successfully in {TimeMs:N0} ms", Stopwatch.GetElapsedTime(now).TotalMilliseconds);
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

// TODO: Make these optional?

void AddInfractionServices(IServiceCollection services)
{
    services.AddHttpClient
    (
        "Infractions",
        (s, c) =>
        {
            var address = s.GetService<IConfiguration>()!["Kobalt:InfractionsApiUrl"] ??
                          throw new KeyNotFoundException("The Phishing API url was not configured.");

            c.BaseAddress = new Uri(address);
        }
    );

    services.AddSingleton<InfractionAPIService>();
}

void AddPhishingServices(IServiceCollection services)
{
    services.AddHttpClient
    (
        "Phishing",
        (s, c) =>
        {
            var address = s.GetService<IConfiguration>()!["Kobalt:PhishingApiUrl"] ??
                          throw new KeyNotFoundException("The Phishing API url was not configured.");

            c.BaseAddress = new Uri(address);
        }
    );

    services.AddScoped<PhishingAPIService>();
    services.AddScoped<PhishingDetectionService>();
}