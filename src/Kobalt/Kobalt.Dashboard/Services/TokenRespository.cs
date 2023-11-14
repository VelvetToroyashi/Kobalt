using System.Collections.Concurrent;
using Kobalt.Dashboard.Extensions;
using Microsoft.AspNetCore.Authentication.OAuth;

namespace Kobalt.Dashboard.Services;

public record DiscordOAuth2Token
(
    string? AccessToken,
    string? RefreshToken,
    string? TokenType,
    DateTimeOffset? ExpiresAt
);


/// <inheritdoc />
public class TokenRespository : ITokenRepository
{
    private readonly ConcurrentDictionary<ulong, DiscordOAuth2Token> _tokens = new();

    public DiscordOAuth2Token? GetToken(ulong userID) => _tokens.TryGetValue(userID, out var token) ? token : null;

    public void SetToken(OAuthCreatingTicketContext context)
    {
        var store = new DiscordOAuth2Token(context.AccessToken, context.RefreshToken, context.TokenType, context.GetTokenExpiry());
        _tokens.AddOrUpdate(ulong.Parse(context.User.GetProperty("id").GetString()!), store, (_, _) => store);
    }

    public void RevokeToken(ulong userID) => _tokens.TryRemove(userID, out _);
}

/// <summary>
/// Represents a central repository for tokens.
/// </summary>
public interface ITokenRepository
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

    /// <summary>
    /// Revokes the token for the specified user.
    /// </summary> 
    public void RevokeToken(ulong userID);
}