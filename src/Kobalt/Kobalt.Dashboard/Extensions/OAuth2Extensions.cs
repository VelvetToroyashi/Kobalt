using Microsoft.AspNetCore.Authentication.OAuth;

namespace Kobalt.Dashboard.Extensions;

public static class OAuth2Extensions
{
    private const string DiscordTokenExpiryKey = ".Token.expires_at";

    public static DateTimeOffset? GetTokenExpiry(string? value)
    {
        return DateTimeOffset.TryParse(value, out var tokenExpiry)
        ? tokenExpiry
        : null;
    }

    public static DateTimeOffset? GetTokenExpiry(this OAuthCreatingTicketContext context)
    {
        return GetTokenExpiry(context.Properties.Items[DiscordTokenExpiryKey]);
    }
    
}