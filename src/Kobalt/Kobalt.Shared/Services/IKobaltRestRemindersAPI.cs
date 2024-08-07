using Kobalt.Shared.DTOs.Reminders;
using Refit;
using Remora.Rest.Core;

namespace Kobalt.Shared.Services;

/// <summary>
/// Represents a REST API for interacting with Kobalt's Reminder API.
/// </summary>
public interface IKobaltRestRemindersAPI
{
    /// <summary>
    /// Creates a new reminder.
    /// </summary>
    /// <param name="userID">The ID of the user to create a reminder for.</param>
    /// <param name="payload">The data of the reminder.</param>
    /// <returns>The created reminder.</returns>
    [Post("/api/reminders/{userID}")]
    public Task<ReminderCreationPayload> CreateReminderAsync(Snowflake userID, [Body] ReminderCreatePayload payload);

    /// <summary>
    /// Gets all reminders for a user.
    /// </summary>
    /// <param name="userID">The ID of the user to get reminders for.</param>
    /// <returns>The user's reminders.</returns>
    [Get("/api/reminders/{userID}")]
    public Task<IReadOnlyList<ReminderDTO>> GetRemindersAsync(Snowflake userID);

    /// <summary>
    /// Deletes a reminder.
    /// </summary>
    /// <param name="userID">The ID of the user deleting a reminder.</param>
    /// <param name="ids">The IDs of the reminder being deleted.</param>
    [Delete("/api/reminders/{userID}")]
    public Task DeleteRemindersAsync(Snowflake userID, [Body] [AliasAs("reminderIDs")] IReadOnlyList<int> ids);
}
