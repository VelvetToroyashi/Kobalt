using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Caching.Abstractions;
using Remora.Discord.Caching.Abstractions.Services;
using Remora.Discord.Rest.Extensions;
using Remora.Rest;
using Remora.Rest.Core;

namespace Kobalt.Bot.Auth;

public class DiscordAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    public const string SchemeName = "Discord";
    
    public DiscordAuthenticationSchemeOptions()
    {
        ClaimsIssuer = "Discord";
    }
}

public class DiscordAuthenticationHandler
(
    IRestHttpClient rest,
    ICacheProvider cache, 
    IUser self,
    IOptionsMonitor<DiscordAuthenticationSchemeOptions> options,
    UrlEncoder encoder
)
: AuthenticationHandler<DiscordAuthenticationSchemeOptions>(options, NullLoggerFactory.Instance, encoder)
{
    protected async override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = Context.Request.Headers.Authorization.FirstOrDefault()?.Split(' ').LastOrDefault();

        if (token is null)
        {
            return AuthenticateResult.Fail("Missing token.");
        }

        var cacheKey = CacheKey.LocalizedStringKey("kobalt-token-store", token); 
        var tokenResult = await cache.RetrieveAsync<string>(cacheKey);

        if (tokenResult.IsSuccess)
        {
            return AuthenticateResult.Success(CreateTicket(DiscordSnowflake.New(ulong.Parse(tokenResult.Entity)), token));
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
            return AuthenticateResult.Fail("Invalid token.");
        }
        
        var app = tokenValidationResult.Entity.Application;

        if (app.ID.Value == self.ID)
        {
            await cache.CacheAsync(cacheKey, token, new CacheEntryOptions { AbsoluteExpiration = tokenValidationResult.Entity.Expires });
            return AuthenticateResult.Success(CreateTicket(tokenValidationResult.Entity.User.ID, token));
        }
        
        return AuthenticateResult.Fail("Invalid token.");
    }

    private AuthenticationTicket CreateTicket(Snowflake userID, string token)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userID.ToString()), new Claim("kobalt:user:token", token) };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        
        return new AuthenticationTicket(principal, Scheme.Name);
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