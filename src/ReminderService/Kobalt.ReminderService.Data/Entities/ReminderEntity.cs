using Kobalt.Infrastructure.DTOs.Reminders;
using Remora.Discord.API;

namespace Kobalt.ReminderService.Data.Entities;

/// <summary>
/// Represents a reminder.
/// </summary>
public class ReminderEntity
{
    /// <summary>
    /// The ID of the reminder.
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// The ID of the user that created this reminder.
    /// </summary>
    public ulong AuthorID { get; set; }
    
    /// <summary>
    /// The ID of the channel that this reminder was created in.
    /// </summary>
    public ulong ChannelID { get; set; }
    
    /// <summary>
    /// The ID of the guild the reminder was set in, if any.
    /// </summary>
    public ulong? GuildID { get; set; }
    
    /// <summary>
    /// The content of the reminder.
    /// </summary>
    public string ReminderContent { get; set; } = null!;
    
    /// <summary>
    /// When the reminder was created.
    /// </summary>
    public DateTimeOffset Creation { get; set; }
    
    /// <summary>
    /// When the reminder will expire.
    /// </summary>
    public DateTimeOffset Expiration { get; set; }
    
    /// <summary>
    /// The ID of the message that this reminder is replying to, if any.
    /// </summary>
    public ulong? ReplyMessageID { get; set; }

    /// <summary>
    /// Implicitly converts a <see cref="ReminderEntity"/> to a <see cref="ReminderDTO"/>.
    /// </summary>
    /// <param name="self">The entity.</param>
    /// <returns>A DTO.</returns>
    public static explicit operator ReminderDTO(ReminderEntity self) => new
    (
        self.Id,
        DiscordSnowflake.New(self.AuthorID),
        DiscordSnowflake.New(self.ChannelID),
        self.GuildID is null ? null : DiscordSnowflake.New(self.GuildID.Value),
        self.ReminderContent, 
        self.Creation,
        self.Expiration,
        self.ReplyMessageID is null ? null : DiscordSnowflake.New(self.ReplyMessageID.Value)
    );
}
