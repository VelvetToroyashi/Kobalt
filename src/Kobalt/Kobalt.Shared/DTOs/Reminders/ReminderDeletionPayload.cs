namespace Kobalt.Shared.DTOs.Reminders;

/// <summary>
/// Represents the result of cancelling one or more reminders.
/// </summary>
/// <param name="CancelledReminders">The ID of reminders that were successfully cancelled.</param>
/// <param name="InvalidReminders">The ID of reminders that were unsuccessfully cancelled (Incorrect user or non-existant reminder).</param>
public record ReminderDeletionPayload(List<int> CancelledReminders, List<int> InvalidReminders);
