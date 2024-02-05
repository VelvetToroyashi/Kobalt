using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Caching.Abstractions.Services;
using Remora.Discord.Rest.Extensions;
using Remora.Rest;
using Remora.Rest.Core;

namespace Kobalt.Bot.Auth;

public class MustManageGuildRequirement : IAuthorizationRequirement;

public class GuildManagementAuthorizationHandler(IRestHttpClient rest, ICacheProvider cache) : AuthorizationHandler<MustManageGuildRequirement, Snowflake>
{
    public const string PolicyName = "MustManageGuild";
    
    protected async override Task HandleRequirementAsync(AuthorizationHandlerContext context, MustManageGuildRequirement requirement, Snowflake resource)
    {
        var memberResult = await rest.GetAsync<IGuildMember>
        (
            $"guilds/{resource}/members/@me",
            b => b.SkipAuthorization()
                  .AddHeader("Bearer", context.User.FindFirstValue("kobalt:user:token")!)
                  .WithRateLimitContext(cache)
        );
        
        if (!memberResult.IsSuccess)
        {
            context.Fail();
            return;
        }
        
        if (!memberResult.Entity.Permissions.OrDefault(DiscordPermissionSet.Empty).HasPermission(DiscordPermission.ManageGuild))
        {
            context.Fail();
            return;
        }
        
        context.Succeed(requirement);
    }
}