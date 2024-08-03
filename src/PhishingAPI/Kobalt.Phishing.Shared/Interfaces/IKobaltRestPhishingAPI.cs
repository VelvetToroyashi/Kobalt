using Kobalt.Phishing.Shared.Models;
using Refit;
using Remora.Rest.Core;

namespace Kobalt.Phishing.Shared.Interfaces;

/// <summary>
/// Represents a REST API for interacting with Kobalt's Phishing API.
/// </summary>
public interface IKobaltRestPhishingAPI
{
    /// <summary>
    /// Checks if a user is suspicious.
    /// </summary>
    /// <param name="guildID">The ID of the guild to check for.</param>
    /// <param name="user">The request body.</param>
    /// <returns>A result determining whether the user is suspicious.</returns>
    [Post("/phishing/check/{guildID}/user")]
    public Task<UserPhishingDetectionResult> CheckUserAsync(Snowflake guildID, [Body] CheckUserRequest user);

    /// <summary>
    /// Checks if a domain is suspicious.
    /// </summary>
    /// <param name="domains">The domains to check.</param>
    /// <returns>A list of domains that are suspicious.</returns>
    [Post("/phishing/check/domains")]
    public Task<UserPhishingDetectionResult> CheckLinksAsync([Body] IReadOnlyList<string> domains);

    /// <summary>
    /// Creates a new record for a suspicious username on a guild.
    /// </summary>
    /// <param name="guildID">The ID of the guild to create a record for.</param>
    /// <param name="request">The data of the request.</param>
    [Put("/phishing/{guildID}/username")]
    public Task CreateSuspiciousUsernameAsync(Snowflake guildID, [Body] SubmitUsernameRequest request);

    /// <summary>
    /// Creates a new record for a suspicious avatar on a guild.
    /// </summary>
    /// <param name="guildID">The ID of the guild to create a record for.</param>
    /// <param name="request">The data of the request.</param>
    [Put("/phishing/{guildID}/avatar")]
    public Task CreateSuspiciousUserAvatarAsync(Snowflake guildID, [Body] SubmitAvatarRequest request);

}
