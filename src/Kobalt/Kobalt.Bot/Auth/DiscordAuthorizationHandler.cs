using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Caching.Abstractions;
using Remora.Discord.Caching.Abstractions.Services;
using Remora.Discord.Rest.Extensions;
using Remora.Rest;

namespace Kobalt.Bot.Auth;

public class DiscordAuthorizationRequirement : IAuthorizationRequirement { }

public class DiscordAuthorizationHandler(IRestHttpClient rest, ICacheProvider cache, IHttpContextAccessor httpContext, IUser self) : AuthorizationHandler<DiscordAuthorizationRequirement>
{
    protected async override Task HandleRequirementAsync(AuthorizationHandlerContext context, DiscordAuthorizationRequirement requirement)
    {
        var token = httpContext.HttpContext.Request.Headers.Authorization.FirstOrDefault();

        if (token is null)
        {
            context.Fail();
            return;
        }

        var cacheKey = CacheKey.LocalizedStringKey("kobalt-token-store", token); 
        var tokenResult = await cache.RetrieveAsync<string>(cacheKey);

        if (tokenResult.IsSuccess)
        {
            context.Succeed(requirement);
            context.User.AddIdentity(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, tokenResult.Entity) }));
        }
        
        var tokenValidationResult = await rest.GetAsync<OAuth2Information>
        (
            "oauth2/@me", 
            b => b.WithRateLimitContext(cache)
                  .SkipAuthorization()
                  .With(req => req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token))
        );

        if (!tokenValidationResult.IsSuccess)
        {
            context.Fail();
            return;
        }
        
        var app = tokenValidationResult.Entity.Application;

        if (app.ID.Value == self.ID)
        {
            context.Succeed(requirement);
            context.User.AddIdentity(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, tokenResult.Entity) }));
            await cache.CacheAsync(cacheKey, token, new CacheEntryOptions { AbsoluteExpiration = tokenValidationResult.Entity.Expires });
        }
    }
}

file record OAuth2Information
(
    [property: JsonPropertyName("application")]
    IPartialApplication Application,
    
    [property: JsonPropertyName("expires")]
    DateTimeOffset Expires,
    
    [property: JsonPropertyName("scopes")]
    IReadOnlyList<string> Scopes,
    
    [property: JsonPropertyName("user")]
    IUser User
);