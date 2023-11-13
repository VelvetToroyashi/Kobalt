using Kobalt.Dashboard.Extensions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;

namespace Kobalt.Dashboard.Services;

public class DiscordAuthenticationStateProvider(ITokenRepository tokenStore, ILoggerFactory loggerFactory)
: RevalidatingServerAuthenticationStateProvider(loggerFactory)
{
    // Adjust this parameter to control the time after which the authentication state will be revalidated.
    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(3);


    protected override Task<bool> ValidateAuthenticationStateAsync
    (
        AuthenticationState authState,
        CancellationToken   cancellationToken
    )
    {
        if (!authState.User.IsAuthenticated())
            return Task.FromResult(false);

        var tokenStoreEntry = tokenStore.GetToken(authState.User.GetUserID());
        return tokenStoreEntry is null 
        ? Task.FromResult(false) 
        : Task.FromResult(tokenStoreEntry.ExpiresAt >= DateTimeOffset.UtcNow);
    }
}