namespace Kobalt.Infrastructure.DTOs.Reminders;

/// <summary>
/// Represents the data to create a reminder.
/// </summary>
/// <param name="ChannelID">The ID of the channel the reminder was set in.</param>
/// <param name="GuildID">The ID of the guild the reminder was set in, if any.</param>
/// <param name="ReminderContent">The content of the reminder.</param>
/// <param name="Expiration">When the reminder expires.</param>
/// <param name="ReplyMessageID">The ID of the message the reminder is in response to, if any.</param>
public record ReminderCreatePayload
(
    ulong ChannelID,
    ulong? GuildID,
    string ReminderContent,
    DateTimeOffset Expiration,
    ulong? ReplyMessageID
);
