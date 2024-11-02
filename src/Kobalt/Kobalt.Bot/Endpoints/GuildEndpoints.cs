using System.Text;
using System.Text.Json;
using Kobalt.Bot.Auth;
using Kobalt.Bot.Data.DTOs;
using Kobalt.Bot.Data.Entities.RoleMenus;
using Kobalt.Bot.Data.MediatR;
using Kobalt.Bot.Data.MediatR.Guilds;
using Kobalt.Bot.Data.MediatR.RoleMenus;
using Kobalt.Bot.Services;
using Kobalt.Infrastructure.Services;
using Kobalt.Infrastructure.Types;
using Kobalt.Shared.Models;
using Kobalt.Shared.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NodaTime;
using Remora.Rest.Core;
using RemoraHTTPInteractions.Services;

namespace Kobalt.Bot.Endpoints;

public static class GuildEndpoints
{
    public static void MapGuildEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet
            (
                "/api/guilds/{guildID}",
                async (HttpContext ctx, IMediator mediator, ulong guildID) =>
                {
                    var jsonSerializer = ctx.RequestServices.GetRequiredService<IOptionsMonitor<JsonSerializerOptions>>().Get("Discord");
                    var getGuildResult = await mediator.Send(new GetGuild.Request(new Snowflake(guildID)));

                    if (getGuildResult is { Entity: { } guild })
                    {
                        return Results.Json(KobaltGuildDTO.FromEntity(guild), jsonSerializer);
                    }
                    else
                    {
                        return Results.NotFound();
                    }
                }
            )
            .RequireAuthorization();

        endpoints.MapPatch
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

                    if (!guildResult.IsDefined(out var guild))
                    {
                        // 5xx, this should be impossible.
                        return Results.StatusCode(500);
                    }

                    var json = await ctx.Request.ReadFromJsonAsync<KobaltGuildDTO>(jsonSerializer);

                    if (json is null)
                    {
                        return Results.BadRequest();
                    }

                    await mediator.Send
                    (
                        new UpdateGuild.AntiPhishing.Request
                        (
                            guild.ID,
                            json.AntiPhishingConfig.ScanUsers,
                            json.AntiPhishingConfig.ScanLinks,
                            json.AntiPhishingConfig.DetectionAction
                        )
                    );

                    await mediator.Send
                    (
                        new UpdateGuild.AntiRaid.Request
                        (
                            guild.ID,
                            json.AntiRaidConfig.IsEnabled,
                            json.AntiRaidConfig.MinimumAccountAgeBypass,
                            json.AntiRaidConfig.AccountFlagsBypass,
                            json.AntiRaidConfig.BaseJoinScore,
                            json.AntiRaidConfig.JoinVelocityScore,
                            json.AntiRaidConfig.MinimumAgeScore,
                            json.AntiRaidConfig.NoAvatarScore,
                            json.AntiRaidConfig.SuspiciousInviteScore,
                            json.AntiRaidConfig.ThreatScoreThreshold,
                            json.AntiRaidConfig.AntiRaidCooldownPeriod,
                            json.AntiRaidConfig.LastJoinBufferPeriod,
                            json.AntiRaidConfig.MinimumAccountAge
                        )
                    );

                    // TODO: Bulk update
                    foreach (var channel in json.LogChannels)
                    {
                        await mediator.Send(new AddOrModifyLoggingChannel.Request(guild.ID, channel.ChannelID, channel.Type));
                    }

                    return Results.NoContent();
                }
            )
            .RequireAuthorization();

        endpoints.MapGet
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

                    if (getRoleMenusResult.IsDefined(out var roleMenus))
                    {
                        return Results.Json(roleMenus.Select(RoleMenuDTO.FromEntity), jsonSerializer);
                    }
                    else
                    {
                        return Results.NotFound();
                    }
                }

            )
            .RequireAuthorization();

        endpoints.MapPost
            (
                "/api/guilds/{guildID}/rolemenus",
                async
                (
                    HttpContext ctx,
                    IMediator mediator,
                    IAuthorizationService auth,
                    RoleMenuService roleMenus,
                    ulong guildID
                ) =>
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
                                )
                                .ToList()
                        )
                    );

                    var result = await mediator.Send(request);

                    return Results.Json(RoleMenuDTO.FromEntity(result), jsonSerializer);
                }
            )
            .RequireAuthorization();

        endpoints.MapPatch
            (
                "/api/guilds/{guildID}/rolemenus/{roleMenuID}",
                async
                (
                    HttpContext ctx,
                    IMediator mediator,
                    IAuthorizationService auth,
                    RoleMenuService roleMenus,
                    ulong guildID,
                    int roleMenuID
                ) =>
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
                                )
                                .ToList()
                        )
                    );

                    var result = await mediator.Send(request);

                    return Results.Json(result, jsonSerializer);
                }
            )
            .RequireAuthorization();

        endpoints.MapPost
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

                if (!hasHeaders ||
                    !DiscordHeaders.VerifySignature
                    (
                        body,
                        timestamp!,
                        signature!,
                        config.Value.Discord.PublicKey!
                    ))
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

        endpoints.MapPatch
            (
                "/users/@me",
                async (HttpContext context, IMediator mediator, IDateTimeZoneProvider dtz, UserSettingsUpdatePayload payload) =>
                {
                    var user = context.User;

                    var isValidTimezone = payload.Timezone.Map(tz => TimeHelper.GetDateTimeZoneFromString(tz, dtz).IsSuccess).OrDefault(true);

                    if (!isValidTimezone)
                    {
                        return Results.BadRequest("Invalid timezone.");
                    }

                    var result = await mediator.Send(new UpdateUser.Request(new Snowflake(ulong.Parse(user.Identity!.Name!)), payload.Timezone, payload.DisplayTimezone));

                    return Results.Ok();
                }
            )
            .RequireAuthorization(auth => auth.RequireAuthenticatedUser());
    }
}
