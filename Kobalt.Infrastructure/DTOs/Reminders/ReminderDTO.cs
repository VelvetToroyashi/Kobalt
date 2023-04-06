using Remora.Rest.Core;

namespace Kobalt.Infrastructure.DTOs.Reminders;

/// <summary>
/// Represents a reminder.
/// </summary>
/// <param name="Id">The ID of the reminder.</param>
/// <param name="AuthorID">The ID of the user that created the reminder.</param>
/// <param name="ChannelID">The ID of the channel the reminder was set in.</param>
/// <param name="ReplyContent">The content of the reminder.</param>
/// <param name="Creation">When the reminder was created.</param>
/// <param name="Expiration">When the reminder expires.</param>
/// <param name="ReplyMessageID">The ID of the message the reminder is in response to, if any.</param>
public record ReminderDTO
(
    int Id,
    Snowflake AuthorID,
    Snowflake ChannelID,
    string ReplyContent,
    DateTimeOffset Creation,
    DateTimeOffset Expiration,
    Snowflake? ReplyMessageID
);
