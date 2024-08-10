using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Kobalt.Bot.Auth;
using Kobalt.Bot.Autocomplete;
using Kobalt.Bot.Commands;
using Kobalt.Bot.Data;
using Kobalt.Bot.Data.DTOs;
using Kobalt.Bot.Data.Entities.RoleMenus;
using Kobalt.Bot.Data.MediatR;
using Kobalt.Bot.Data.MediatR.Guilds;
using Kobalt.Bot.Data.MediatR.RoleMenus;
using Kobalt.Bot.Handlers;
using Kobalt.Bot.Services;
using Kobalt.Bot.Services.Discord;
using Kobalt.Infractions.Shared.Interfaces;
using Kobalt.Infrastructure;
using Kobalt.Infrastructure.Extensions;
using Kobalt.Infrastructure.Services;
using Kobalt.Infrastructure.Services.Booru;
using Kobalt.Infrastructure.Types;
using Kobalt.Phishing.Shared.Interfaces;
using Kobalt.Shared.Extensions;
using Kobalt.Shared.Models;
using Kobalt.Shared.Services;
using Kobalt.Shared.Types;
using MassTransit.Configuration;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NodaTime;
using Polly;
using Refit;
using Remora.Commands.Extensions;
using Remora.Discord.API.Abstractions.Gateway.Commands;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Gateway.Commands;
using Remora.Discord.Caching.Abstractions.Services;
using Remora.Discord.Caching.Extensions;
using Remora.Discord.Caching.Redis.Extensions;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Responders;
using Remora.Discord.Extensions.Extensions;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Extensions;
using Remora.Discord.Interactivity.Extensions;
using Remora.Discord.Rest;
using Remora.Discord.Rest.Extensions;
using Remora.Rest;
using Remora.Rest.Core;
using RemoraDelegateDispatch.Extensions;
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

builder.Host.AddStartupTaskSupport();

builder.Services.AddSingleton<IAuthorizationHandler, GuildManagementAuthorizationHandler>();

builder.Services.AddAuthentication(DiscordAuthenticationSchemeOptions.SchemeName)
       .AddScheme<DiscordAuthenticationSchemeOptions, DiscordAuthenticationHandler>(DiscordAuthenticationSchemeOptions.SchemeName, null);

builder.Services.AddAuthorization(auth =>
    {
        auth.AddPolicy(GuildManagementAuthorizationHandler.PolicyName, policy => policy.Requirements.Add(new MustManageGuildRequirement()));
    }
);

builder.Services.AddSingleton(TimeProvider.System);

var host = builder.Build();

host.UseAuthentication();
host.UseAuthorization();

#region Endpoints

host.MapGet
(
    "/api/guilds/{guildID}",
    async (HttpContext ctx, IMediator mediator, ulong guildID) =>
    {
        var jsonSerializer = ctx.RequestServices.GetRequiredService<IOptionsMonitor<JsonSerializerOptions>>().Get("Discord");
        var getGuildResult = await mediator.Send(new GetGuild.Request(new Snowflake(guildID)));

        if (getGuildResult is { Entity: {} guild })
        {
            return Results.Json(KobaltGuildDTO.FromEntity(guild), jsonSerializer);
        }
        else
        {
            return Results.NotFound();
        }
    }
).RequireAuthorization();

host.MapPatch
(
    "/api/guilds/{guildID}",
    async (HttpContext ctx, IMediator mediator, IAuthorizationService auth, ulong guildID) =>
    {
        var jsonSerializer = ctx.RequestServices.GetRequiredService<IOptionsMonitor<JsonSerializerOptions>>().Get("Discord");
        var authorization = await auth.AuthorizeAsync(ctx.User, new Snowflake(guildID), GuildManagementAuthorizationHandler.PolicyName);

        if (!authorization.Succeeded)
        {
            return Results.Forbid();
        }

        var guildResult = await mediator.Send(new GetGuild.Request(new Snowflake(guildID)));

        if (guildResult is not { Entity: {} guild })
        {
            // 5xx, this should be impossible.
            return Results.StatusCode(500);
        }

        var json = await ctx.Request.ReadFromJsonAsync<KobaltGuildDTO>(jsonSerializer);

        if (json is null)
        {
            return Results.BadRequest();
        }

        await mediator.Send(new UpdateGuild.AntiPhishing.Request(guild.ID, json.AntiPhishingConfig.ScanUsers, json.AntiPhishingConfig.ScanLinks, json.AntiPhishingConfig.DetectionAction));
        await mediator.Send(new UpdateGuild.AntiRaid.Request(guild.ID, json.AntiRaidConfig.IsEnabled, json.AntiRaidConfig.MinimumAccountAgeBypass, json.AntiRaidConfig.AccountFlagsBypass, json.AntiRaidConfig.BaseJoinScore, json.AntiRaidConfig.JoinVelocityScore, json.AntiRaidConfig.MinimumAgeScore, json.AntiRaidConfig.NoAvatarScore, json.AntiRaidConfig.SuspiciousInviteScore, json.AntiRaidConfig.ThreatScoreThreshold, json.AntiRaidConfig.AntiRaidCooldownPeriod, json.AntiRaidConfig.LastJoinBufferPeriod, json.AntiRaidConfig.MinimumAccountAge));

        // TODO: Bulk update
        foreach (var channel in json.LogChannels)
        {
            await mediator.Send(new AddOrModifyLoggingChannel.Request(guild.ID, channel.ChannelID, channel.Type));
        }

        return Results.NoContent();
    }
).RequireAuthorization();

host.MapGet
(
    "/api/guilds/{guildID}/rolemenus",
    async (HttpContext ctx, IMediator mediator, IAuthorizationService auth, ulong guildID) =>
    {
        var authorization = await auth.AuthorizeAsync(ctx.User, new Snowflake(guildID), GuildManagementAuthorizationHandler.PolicyName);

        if (!authorization.Succeeded)
        {
            return Results.Forbid();
        }

        var jsonSerializer = ctx.RequestServices.GetRequiredService<IOptionsMonitor<JsonSerializerOptions>>().Get("Discord");
        var getRoleMenusResult = await mediator.Send(new GetAllRoleMenus.Request(new Snowflake(guildID)));

        if (getRoleMenusResult is { Entity: {} roleMenus })
        {
            return Results.Json(roleMenus.Select(RoleMenuDTO.FromEntity), jsonSerializer);
        }
        else
        {
            return Results.NotFound();
        }
    }

).RequireAuthorization();

host.MapPost
(
    "/api/guilds/{guildID}/rolemenus",
    async (HttpContext ctx, IMediator mediator, IAuthorizationService auth, RoleMenuService roleMenus, ulong guildID) =>
    {
        var authorization = await auth.AuthorizeAsync(ctx.User, new Snowflake(guildID), GuildManagementAuthorizationHandler.PolicyName);

        if (!authorization.Succeeded)
        {
            return Results.Forbid();
        }

        var jsonSerializer = ctx.RequestServices.GetRequiredService<IOptionsMonitor<JsonSerializerOptions>>().Get("Discord");
        var data = await ctx.Request.ReadFromJsonAsync<RoleMenuDTO>(jsonSerializer);

        if (data is null)
        {
            return Results.BadRequest();
        }

        var request = new CreateRoleMenu.Request
        (
            data.Name,
            data.Description,
            data.ChannelID,
            data.GuildID,
            data.MaxSelections,
            new Optional<IReadOnlyList<RoleMenuOptionEntity>>
            (
                data.Options.Select
                (
                    o => new RoleMenuOptionEntity
                    {
                        Name = data.Name,
                        RoleID = o.RoleID,
                        Description = data.Description,
                        MutuallyExclusiveRoles = o.MutuallyExclusiveRoleIDs.ToList(),
                        MutuallyInclusiveRoles = o.MutuallyInclusiveRoleIDs.ToList(),
                    }
                ).ToList()
            )
        );

        var result = await mediator.Send(request);
        return Results.Json(RoleMenuDTO.FromEntity(result), jsonSerializer);
    }
).RequireAuthorization();

host.MapPatch
(
    "/api/guilds/{guildID}/rolemenus/{roleMenuID}",
    async (HttpContext ctx, IMediator mediator, IAuthorizationService auth, RoleMenuService roleMenus, ulong guildID, int roleMenuID) =>
    {
        var authorization = await auth.AuthorizeAsync(ctx.User, new Snowflake(guildID), GuildManagementAuthorizationHandler.PolicyName);

        if (!authorization.Succeeded)
        {
            return Results.Forbid();
        }

        var jsonSerializer = ctx.RequestServices.GetRequiredService<IOptionsMonitor<JsonSerializerOptions>>().Get("Discord");
        var data = await ctx.Request.ReadFromJsonAsync<RoleMenuDTO>(jsonSerializer);

        if (data is null)
        {
            return Results.BadRequest();
        }

        var request = new UpdateRoleMenu.Request
        (
            roleMenuID,
            new(guildID),
            data.Name,
            data.Description,
            data.MaxSelections,
            default,
            new Optional<IReadOnlyList<RoleMenuOptionEntity>>
            (
                data.Options.Select
                (
                    o => new RoleMenuOptionEntity
                    {
                        Name = data.Name,
                        RoleID = o.RoleID,
                        Description = data.Description,
                        MutuallyExclusiveRoles = o.MutuallyExclusiveRoleIDs.ToList(),
                        MutuallyInclusiveRoles = o.MutuallyInclusiveRoleIDs.ToList(),
                    }
                ).ToList()
            )
        );

        var result = await mediator.Send(request);
        return Results.Json(result, jsonSerializer);
    }
).RequireAuthorization();

host.MapPost
(
    "/interaction",
    async (HttpContext ctx, WebhookInteractionHelper handler, IOptions<KobaltConfig> config) =>
    {
        if (!config.Value.Bot.EnableHTTPInteractions)
        {
            ctx.Response.StatusCode = 403;
            return;
        }

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

host.MapPatch("/users/@me", async (HttpContext context, IMediator mediator, IDateTimeZoneProvider dtz, UserSettingsUpdatePayload payload) => {
    var user = context.User;

    var isValidTimezone = payload.Timezone.Map(tz => TimeHelper.GetDateTimeZoneFromString(tz, dtz).IsSuccess).OrDefault(true);

    if (!isValidTimezone)
    {
        return Results.BadRequest("Invalid timezone.");
    }

    var result = await mediator.Send(new UpdateUser.Request(new Snowflake(ulong.Parse(user.Identity!.Name!)), payload.Timezone, payload.DisplayTimezone));
    return Results.Ok();
}).RequireAuthorization(auth => auth.RequireAuthenticatedUser());
#endregion

await ApplyKobaltEmojisAsync(host.Services);
await host.RunAsync();

#region Local Functions

void ConfigureRedis(IConfiguration config, IServiceCollection services)
{
    var connString = config.GetConnectionString("Redis") ?? throw new InvalidOperationException("Redis connection string not found");
    services.AddDiscordRedisCaching(s => s.Configuration = connString);

    var multiplexer = ConnectionMultiplexer.Connect(connString);
    services.AddSingleton<IConnectionMultiplexer>(multiplexer);
}

void ConfigureKobaltBotServices(IConfiguration hostConfig, IServiceCollection services)
{
    var config = hostConfig.Get<KobaltConfig>()!;
    services.AddSingleton(Options.Create(config));

    var token = config.Discord.Token;

    services.AddDiscordGateway(_ => token);
    services.AddInteractivity();

    if (config.Bot.EnableHTTPInteractions)
    {
        services.AddHttpInteractions();
    }
    services.AddDiscordCommands(true);
    services.AddPostExecutionEvent<PostExecutionHandler>();
    services.AddHostedService<KobaltDiscordGatewayService>();
    services.Configure<InteractionResponderOptions>(s => s.UseEphemeralResponses = true);
    services.AddOffsetServices();

    services.AddTransient<IChannelLoggerService, ChannelLoggerService>();
    services.AddTransient<ImageOverlayService>();

    services.AddHttpClient("booru");
    services.AddTransient<BooruSearchService>();

    services.AddMediatR(s => s.RegisterServicesFromAssemblyContaining<KobaltContext>());

    services.AddDbContextFactory<KobaltContext>("Kobalt");

    var asm = Assembly.GetExecutingAssembly();

    services.AddRespondersFromAssembly(asm);
    services.AddInteractivityFromAssembly(asm);
    services.AddCommandGroupsFromAssembly(asm, typeFilter: t => !t.IsNested && !t.CustomAttributes.Any(t => t.AttributeType == typeof(SkipAssemblyDiscoveryAttribute)));

    services.AddSingleton<AntiRaidV2Service>();
    services.AddSingleton<ChannelWatcherService>();
    services.AddSingleton<MessagePurgeService>();

    services.AddAutocompleteProvider<ReminderAutoCompleteProvider>()
            .AddAutocompleteProvider<RoleMenuAutocompleteProvider>();

    services.AddSingleton<RoleMenuService>();

    services.AddRabbitMQ();

    AddReminderServices(services, config);
    AddPhishingServices(services, config);
    AddInfractionServices(services, config);

    // TODO: Make redis stub so that an in-memory shim can be used instead.
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
    services.AddSingleton(KobaltBot.Policy);

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

    services.AddDelegateResponders();
}

static async Task ApplyKobaltEmojisAsync(IServiceProvider services)
{
    // TODO: Use app API when that PR gets merged.

    var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Kobalt");

    var restClient = services.GetRequiredService<IRestHttpClient>();
    var tokenStore = services.GetRequiredService<IAsyncTokenStore>();
    var token = await tokenStore.GetTokenAsync(default);

    var jsonOptions = services.GetRequiredService<IOptionsMonitor<JsonSerializerOptions>>().Get("Discord");

    var id = Encoding.UTF8.GetString(Convert.FromBase64String(token.Split('.')[0] + "=="));

    logger.LogDebug("Loading Kobalt emojis...");

    var emojiResult = await restClient.GetAsync<JsonDocument>
    (
        $"applications/{id}/emojis",
        b => b.WithRateLimitContext(services.GetRequiredService<ICacheProvider>())
    );

    if (!emojiResult.IsDefined(out var emojiDoc))
    {
        logger.LogError("Failed to get emojis: {Error}", emojiResult.Error);
        return;
    }

    logger.LogDebug("Loaded {Count} emojis", emojiDoc.RootElement.SelectElement("items")!.Value.GetArrayLength());

    var fields = typeof(KobaltEmoji).GetFields(BindingFlags.Public | BindingFlags.Static);
    var emojis = emojiDoc.RootElement.SelectElement("items")!.Value.Deserialize<IReadOnlyList<IEmoji>>(jsonOptions)!;

    foreach (var field in fields)
    {
        var emoji = emojis.FirstOrDefault(e => e.Name!.Equals(field.Name, StringComparison.OrdinalIgnoreCase));

        if (emoji is null)
        {
            continue;
        }

        field.SetValue(null, $"<:{emoji.Name}:{emoji.ID}>");
    }
}

static RefitSettings GetRefitSettings(IServiceProvider provider) => new()
{
    ContentSerializer = new SystemTextJsonContentSerializer
    (
        provider.GetRequiredService<IOptionsMonitor<JsonSerializerOptions>>().Get("Discord")
    )
};

void AddInfractionServices(IServiceCollection services, KobaltConfig config)
{
    if (!config.Bot.EnableInfractions)
    {
        Log.Information("KOBALT_INFRACTIONS_ENABLED is 'false'; infraction services will be unavailable");
        return;
    }

    services.AddCommandTree().WithCommandGroup<ModerationCommands>();

    services.AddRefitClient<IInfractionAPI>(GetRefitSettings)
            .ConfigureHttpClient
            (
                (c) =>
                {
                    var address = config.Bot.InfractionsUrl ??
                                  throw new KeyNotFoundException("The Infractions API url was not configured.");

                    c.BaseAddress = new Uri(address);
                }
            );

    services.AddSingleton<InfractionAPIService>();
    services.RegisterConsumer<InfractionAPIService>();
}

void AddPhishingServices(IServiceCollection services, KobaltConfig config)
{
    if (!config.Bot.EnablePhishing)
    {
        Log.Information("KOBALT_PHISHING_ENABLED is 'false'; anti-phishing services will be unavailable");
        return;
    }

    services.AddRefitClient<IKobaltRestPhishingAPI>(GetRefitSettings)
            .AddPolicyHandler(KobaltBot.Policy)
            .ConfigureHttpClient
            (
                (s, c) =>
                {
                    var address = config.Bot.PhishingUrl ??
                                  throw new KeyNotFoundException("The Phishing API url was not configured.");

                    c.BaseAddress = new Uri(address);
                }
            );

    services.AddScoped<PhishingAPIService>();
    services.AddScoped<PhishingDetectionService>();

    services.AddDelegateResponder<IGuildMemberAdd>
    (
        (IGuildMemberAdd member, PhishingDetectionService phishing, CancellationToken ct)
        => phishing.HandleAsync(member.User.Value, member.GuildID, ct)
    );

    services.AddDelegateResponder<IGuildMemberUpdate>
    (
        (IGuildMemberUpdate member, PhishingDetectionService phishing, CancellationToken ct)
        => phishing.HandleAsync(member.User, member.GuildID, ct)
    );

    services.AddDelegateResponder<IMessageCreate>
    (
        (IMessageCreate message, PhishingDetectionService phishing, CancellationToken ct)
        => phishing.HandleAsync(message, message.GuildID, ct)
    );
}

void AddReminderServices(IServiceCollection serviceCollection, KobaltConfig config)
{

    if (!config.Bot.EnableReminders)
    {
        Log.Information("KOBALT_REMINDERS_ENABLED is 'false'; reminder services will be unavailable");
        return;
    }

    serviceCollection.AddCommandTree()
                     .WithCommandGroup<ReminderCommands>()
                     .WithCommandGroup<ReminderContextCommands>();

    serviceCollection.AddRefitClient<IKobaltRestRemindersAPI>(GetRefitSettings)
                     .AddPolicyHandler(KobaltBot.Policy)
                     .ConfigureHttpClient
                     (
                         (s, c) =>
                         {
                             var address = config.Bot.RemindersUrl ??
                                           throw new KeyNotFoundException("The Reminders API url was not configured.");

                             c.BaseAddress = new Uri(address);
                         }
                     );

    serviceCollection.AddSingleton<ReminderAPIService>();
    serviceCollection.RegisterConsumer<ReminderAPIService>();
}
#endregion

file class KobaltBot
{
    public static readonly IAsyncPolicy<HttpResponseMessage> Policy =
        Policy<HttpResponseMessage>
        .Handle<HttpRequestException>()
        .WaitAndRetryAsync
        (
            5,
            i => TimeSpan.FromSeconds(Math.Log(i * i) + 1)
        );
}
