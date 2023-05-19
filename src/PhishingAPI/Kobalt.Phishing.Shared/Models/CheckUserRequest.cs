using Remora.Rest.Core;

namespace Kobalt.Phishing.Shared.Models;

/// <summary>
/// Represents a request to check a user's profile for phishing.
/// </summary>
/// <param name="UserID">The ID of the user.</param>
/// <param name="Username">The username of the user.</param>
/// <param name="AvatarHash">The user's avatar hash, if any.</param>
public record CheckUserRequest
(
    Snowflake UserID,
    string Username,
    string? AvatarHash
);
