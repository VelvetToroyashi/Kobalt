namespace Kobalt.Bot.Data.Entities.Phishing;

/// <summary>
/// Represents a suspicious avatar
/// </summary>
public class SuspiciousAvatar
{
    /// <summary>
    /// The id of this entity.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The associated guild with this avatar, if any.
    /// </summary>
    public required ulong? GuildID { get; set; }

    /// <summary>
    /// The MD5 hash of this avatar, if any.
    /// </summary>
    public required string? Md5Hash { get; set; }

    /// <summary>
    /// Who added this avatar.
    /// </summary>
    public required ulong AddedBy { get; set; }

    /// <summary>
    /// When this avatar was added.
    /// </summary>
    public required DateTimeOffset AddedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The source of this avatar, such as Discord.
    /// </summary>
    public required string Source { get; set; }

    /// <summary>
    /// The category of this avatar, e.g. `discord_modmail`.
    /// </summary>
    public required string Category { get; set; }

    /// <summary>
    /// The phash of this avatar.
    /// </summary>
    public required byte[] Phash { get; set; }
}
