using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Kobalt.Bot.Auth;
using Kobalt.Bot.Autocomplete;
using Kobalt.Bot.Data;
using Kobalt.Bot.Data.DTOs;
using Kobalt.Bot.Data.Entities.RoleMenus;
using Kobalt.Bot.Data.MediatR;
using Kobalt.Bot.Data.MediatR.Guilds;
using Kobalt.Bot.Data.MediatR.RoleMenus;
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
using MassTransit.Configuration;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Polly;
using Remora.Commands.Extensions;
using Remora.Discord.API.Abstractions.Gateway.Commands;
using Remora.Discord.API.Abstractions.Gateway.Events;
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
using Remora.Rest.Core;
using RemoraDelegateDispatch.Extensions;
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

builder.Services.AddSingleton<IAuthorizationHandler, DiscordAuthorizationHandler>();

builder.Services.AddAuthentication(DiscordAuthenticationSchemeOptions.SchemeName)
       .AddScheme<DiscordAuthenticationSchemeOptions, DiscordAuthenticationHandler>(DiscordAuthenticationSchemeOptions.SchemeName, null);

builder.Services.AddAuthorization(auth => auth.AddPolicy(DiscordAuthorizationHandler.PolicyName, policy => policy.Requirements.Add(new MustManageGuildRequirement())));

var host = builder.Build();

host.UseAuthentication();
host.UseAuthorization();

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
        var authorization = await auth.AuthorizeAsync(ctx.User, new Snowflake(guildID), DiscordAuthorizationHandler.PolicyName);
        
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
        var authorization = await auth.AuthorizeAsync(ctx.User, new Snowflake(guildID), DiscordAuthorizationHandler.PolicyName);
        
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
        var authorization = await auth.AuthorizeAsync(ctx.User, new Snowflake(guildID), DiscordAuthorizationHandler.PolicyName);
        
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
        var authorization = await auth.AuthorizeAsync(ctx.User, new Snowflake(guildID), DiscordAuthorizationHandler.PolicyName);
            
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

host.MapPost("/interaction", async (HttpContext ctx, WebhookInteractionHelper handler, IOptions<KobaltConfig> config) =>
    {
        var hasHeaders = DiscordHeaders.TryExtractHeaders(ctx.Request.Headers, out var timestamp, out var signature);
        using var sr = new StreamReader(ctx.Request.Body);
        var body = await sr.ReadToEndAsync();

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

    services.AddDiscordGateway(_ => token);
    services.AddInteractivity();
    //services.AddHTTPInteractionAPIs();
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

    services.AddSingleton<RoleMenuService>();

    services.AddRabbitMQ();
    
    services.AddSingleton<ReminderAPIService>();
    services.RegisterConsumer<ReminderAPIService>();

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
                        State: "Perhaps there will be a random quote here from time to time.\n Who knows?",
                        Type: ActivityType.Custom
                    )
                }
            );
        }
    );

    services.AddDelegateResponders();

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
    services.RegisterConsumer<InfractionAPIService>();
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