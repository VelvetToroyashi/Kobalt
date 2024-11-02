using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Kobalt.Bot.Auth;
using Kobalt.Bot.Autocomplete;
using Kobalt.Bot.Commands;
using Kobalt.Bot.Data;
using Kobalt.Bot.Endpoints;
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
using Kobalt.Shared.Services;
using Kobalt.Shared.Types;
using MassTransit.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
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
using Remora.Discord.Caching.Services;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Responders;
using Remora.Discord.Extensions.Extensions;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Extensions;
using Remora.Discord.Interactivity.Extensions;
using Remora.Discord.Rest;
using Remora.Discord.Rest.Extensions;
using Remora.Rest;
using RemoraDelegateDispatch.Extensions;
using RemoraHTTPInteractions.Extensions;
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

host.MapGuildEndpoints();
host.MapDiscordWebhookEndpoints();

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
    services.AddPreparationErrorEvent<PostExecutionHandler>();
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

    services.Configure<CacheSettings>(s => s.SetDefaultEvictionAbsoluteExpiration(TimeSpan.Zero));

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
