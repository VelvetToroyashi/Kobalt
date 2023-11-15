using System.Collections.Concurrent;
using Kobalt.Dashboard.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Remora.Discord.Rest;

namespace Kobalt.Dashboard.Services;

public record DiscordOAuth2Token
(
    string? AccessToken,
    string? RefreshToken,
    string? TokenType,
    DateTimeOffset? ExpiresAt
)
{
    public static async Task<DiscordOAuth2Token> FromHttpContext(HttpContext context)
    {
        var results = await Task.WhenAll
        (
            context.GetTokenAsync("access_token"),
            context.GetTokenAsync("refresh_token"),
            context.GetTokenAsync("token_type"),
            context.GetTokenAsync("expires_at")
        );

        DateTimeOffset.TryParse(results[3], out var tokenExpiry);
        return new DiscordOAuth2Token
        (
            results[0],
            results[1],
            results[2],
            tokenExpiry
        );
    }
}

/// <inheritdoc />
public class TokenRepository(IHttpContextAccessor contextAccessor) : ITokenRepository
{
    private readonly ConcurrentDictionary<ulong, DiscordOAuth2Token> _tokens = new();

    public DiscordOAuth2Token? GetToken(ulong userID) => _tokens.TryGetValue(userID, out var token) ? token : null;

    public void SetToken(OAuthCreatingTicketContext context)
    {
        var store = new DiscordOAuth2Token(context.AccessToken, context.RefreshToken, context.TokenType, context.GetTokenExpiry());
        _tokens.AddOrUpdate(ulong.Parse(context.User.GetProperty("id").GetString()!), store, (_, _) => store);
    }
    
    public void SetToken(ulong userID, DiscordOAuth2Token token) => _tokens.AddOrUpdate(userID, token, (_, _) => token);

    public void RevokeToken(ulong userID) => _tokens.TryRemove(userID, out _);

    // Maybe Caleb was on to something with storing the current user token?
    // It seems a little weird though; one wrong request and boom, you're logged in as someone else.
    public string Token => GetCurrentToken();
    public DiscordTokenType TokenType => DiscordTokenType.Bearer;

    private string GetCurrentToken()
    {
        if (contextAccessor.HttpContext is not { } activeContext)
        {
            throw new InvalidOperationException("There is no active HTTP context.");
        }
        
        if (!activeContext.User.Identity!.IsAuthenticated)
        {
            throw new InvalidOperationException("The current user is not authenticated.");
        }
        
        var userID = activeContext.User.GetUserID();
        var token = GetToken(userID);
        
        if (token is null)
        {
            throw new InvalidOperationException("The current user does not have a token.");
        }
        
        return token.AccessToken!;
    }
}

/// <summary>
/// Represents a central repository for tokens.
/// </summary>
public interface ITokenRepository: ITokenStore
{
    /// <summary>
    /// Gets the token for the specified user.
    /// </summary>
    /// <param name="userID">The ID of the user.</param>
    /// <returns>The token, or <see langword="null"/> if no token exists.</returns>
    public DiscordOAuth2Token? GetToken(ulong userID);

    /// <summary>
    /// Sets the token for the specified user.
    /// </summary>
    public void SetToken(OAuthCreatingTicketContext context);
    
    public void SetToken(ulong userID, DiscordOAuth2Token token);

    /// <summary>
    /// Revokes the token for the specified user.
    /// </summary> 
    public void RevokeToken(ulong userID);
}