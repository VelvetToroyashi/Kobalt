using System.Diagnostics.CodeAnalysis;
using Kobalt.Shared.Models.Phishing;

namespace Kobalt.Bot.Data.Entities.Phishing;

public class SuspiciousUsername
{
    public int Id { get; set; }

    /// <summary>
    /// The ID of the guild this username is for, if any.
    /// </summary>
    public ulong? GuildID { get; set; }

    /// <summary>
    /// The pattern for the username, or a username literal.
    /// </summary>
    [NotNull]
    public string? UsernamePattern { get; set; }

    /// <summary>
    /// The way the username should be parsed.
    /// </summary>
    public UsernameParseType ParseType { get; set; }

    /// <summary>
    /// The ID of the user who added this username.
    /// </summary>
    public ulong CreatedBy { get; set; }

    /// <summary>
    /// When this username was added.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
