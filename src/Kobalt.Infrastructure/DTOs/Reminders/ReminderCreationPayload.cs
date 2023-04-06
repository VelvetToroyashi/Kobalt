namespace Kobalt.Infrastructure.DTOs.Reminders;

/// <summary>
/// Represents the return result of creating a reminder.
/// </summary>
/// <param name="Id">The id of the reminder.</param>
/// <param name="Expiration">When the reminder expires</param>
public record ReminderCreationPayload(int Id, DateTimeOffset Expiration);
