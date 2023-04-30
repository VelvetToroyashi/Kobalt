﻿using System.Reflection;
using System.Text;
using Kobalt.Bot.Handlers;
using Kobalt.Bot.Services;
using Kobalt.Bot.Services.Discord;
using Kobalt.Data;
using Kobalt.Infrastructure;
using Kobalt.Infrastructure.Extensions;
using Kobalt.Infrastructure.Services;
using Kobalt.Infrastructure.Services.Booru;
using Kobalt.Infrastructure.Types;
using Kobalt.ReminderService.API.Extensions;
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
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Responders;
using Remora.Discord.Extensions.Extensions;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Extensions;
using Remora.Discord.Interactivity.Extensions;
using RemoraHTTPInteractions.Extensions;
using RemoraHTTPInteractions.Services;
using Serilog;

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

// TODO: replace either with runtimeProperties.json or a config
builder.WebHost.ConfigureKestrel(c => c.ListenLocalhost(builder.Configuration.GetKobaltConfig().ApiPort));
builder.Host.AddStartupTaskSupport();

var host = builder.Build();

initResult = await PluginHelper.InitializePluginsAsync(host.Services);

if (!initResult.IsSuccess)
{
    Log.Fatal("Failed to initialize plugins: {Error}", initResult.Error);
    return;
}

await host.Services.GetRequiredService<IDbContextFactory<KobaltContext>>().CreateDbContext().Database.MigrateAsync();

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
            return;
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

void ConfigureKobaltBotServices(IConfiguration hostConfig, IServiceCollection services)
{
    var config = hostConfig.Get<KobaltConfig>()!;
    services.AddSingleton(Options.Create(config));

    builder.Services.AddMediatR(s => s.RegisterServicesFromAssemblyContaining<KobaltContext>()
                                      .RegisterServicesFromAssemblyContaining<Program>());

    builder.Services.AddDbContextFactory<KobaltContext>("Kobalt");

    var token = config.Discord.Token;

    services.AddDiscordGateway(_ => token);
    services.AddHTTPInteractionAPIs();
    services.AddDiscordCommands(true);
    services.Configure<InteractionResponderOptions>(s => s.UseEphemeralResponses = true);
    services.AddCommandGroupsFromAssembly(Assembly.GetExecutingAssembly(), typeFilter: t => !t.IsNested);
    services.AddInteractivityFromAssembly(Assembly.GetExecutingAssembly());
    services.AddHostedService<KobaltDiscordGatewayService>();
    services.AddTransient<ImageOverlayService>();

    services.AddPostExecutionEvent<PostExecutionHandler>();

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

    services.AddTransient<IChannelLoggerService, ChannelLoggerService>();

    services.AddOffsetServices();
    services.AddMemoryCache();

    services.AddHttpClient("booru");
    services.AddTransient<BooruSearchService>();

    services.AddSingleton<IAsyncPolicy<HttpResponseMessage>>(Policy<HttpResponseMessage>.Handle<HttpRequestException>().WaitAndRetryAsync(5, i => TimeSpan.FromSeconds(Math.Log(i * i) + 1)));

    services.AddCondition<EnsureHierarchyCondition>();

    services.Configure<DiscordGatewayClientOptions>
    (
        options =>
        {
            options.Intents |= GatewayIntents.MessageContents;
            options.Presence = new UpdatePresence
            (
                Status: UserStatus.DND,
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

    services.AddInteractivity();
    services.AddRespondersFromAssembly(Assembly.GetExecutingAssembly());
    services.AddInteractivityFromAssembly(Assembly.GetExecutingAssembly());
}
