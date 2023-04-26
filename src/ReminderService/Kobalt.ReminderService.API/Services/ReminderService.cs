using Kobalt.Infrastructure.DTOs.Reminders;
using Kobalt.ReminderService.Data.Mediator;
using MassTransit;
using Mediator;
using Remora.Results;

namespace Kobalt.ReminderService.API.Services;

/// <summary>
/// A service that dispatches reminders;
/// </summary>
public class ReminderService : IHostedService
{
    private readonly IBus _messageBus;
    private readonly IMediator _mediator;
    private readonly ILogger<ReminderService> _logger;

    private readonly List<ReminderDTO> _reminders = new();
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(1));

    private readonly Task _dispatchTask;
    private readonly CancellationTokenSource _cts = new();

    public ReminderService(IBus bus, IMediator mediator, ILogger<ReminderService> logger)
    {
        _messageBus = bus;
        _mediator = mediator;
        _logger = logger;
        _dispatchTask = DispatchReminders();

        _logger.LogInformation("Reminder service started.");
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cts.Cancel();
        _timer.Dispose();
        return _dispatchTask;
    }

    /// <summary>
    /// Creates a reminder.
    /// </summary>
    /// <param name="authorID">The ID of the user that created the reminder.</param>
    /// <param name="channelID">The ID of the channel the reminder was set in.</param>
    /// <param name="guildID">The ID of the guild the reminder was set in, if any.</param>
    /// <param name="reminderContent">The content of the reminder.</param>
    /// <param name="creation">When the reminder was created.</param>
    /// <param name="expiration">When the reminder expires.</param>
    /// <param name="replyMessageID">The ID of the message the reminder is in response to, if any.</param>
    /// <returns>The created reminder.</returns>
    public async Task<ReminderCreationPayload> CreateReminderAsync(ulong authorID, ulong channelID, ulong? guildID, string reminderContent, DateTimeOffset expiration, ulong? replyMessageID)
    {
        var created = await _mediator.Send(new CreateReminder.Request(authorID, channelID, guildID, reminderContent, expiration, replyMessageID));
        _reminders.Add(created);
        _logger.LogDebug("Added reminder to in-memory list.");

        return new(created.Id, created.Expiration);
    }

    /// <summary>
    /// Removes a reminder both from the database and the in-memory list.
    /// </summary>
    /// <param name="reminderID">The ID of the reminder</param>
    /// <param name="userID">The ID of the user attempting to delete the reminder.</param>
    /// <returns>A result indicating whether the reminder was successfully deleted or not.</returns>
    public async Task<Result> RemoveReminderAsync(int reminderID, ulong userID)
    {
        var reminder = await _mediator.Send(new DeleteReminder.Request(reminderID, userID));

        if (!reminder.IsSuccess)
        {
            return reminder;
        }

        _reminders.RemoveAll(x => x.Id == reminderID);

        return Result.FromSuccess();
    }

    /// <summary>
    /// Dispatches expired reminders in a round-robin fashion, requeing reminders that fail to send.
    /// </summary>
    /// <remarks>
    /// Dead clients are removed from the client list, and the corresponding <see cref="CancellationTokenSource"/> is cancelled.
    /// </remarks>
    private async Task DispatchReminders()
    {
        _logger.LogInformation("Loading reminders...");
        var reminders = await _mediator.Send(new GetAllReminders.Request());

        _reminders.AddRange(reminders);

        _logger.LogInformation("Loaded {ReminderCount} reminders.", _reminders.Count);

        while (await _timer.WaitForNextTickAsync(_cts.Token))
        {
            for (int i = _reminders.Count - 1; i >= 0; i--)
            {
                var reminder = _reminders[i];

                if (reminder.Expiration > DateTimeOffset.UtcNow)
                {
                    continue;
                }

                _reminders.RemoveAt(i);
                _logger.LogInformation("Reminder {ReminderID} has expired, dispatching.", reminder.Id);

                await _messageBus.Publish(reminder, _cts.Token);

                var res = await _mediator.Send(new DeleteReminder.Request(reminder.Id, reminder.AuthorID.Value));

                if (!res.IsSuccess)
                {
                    _logger.LogWarning("Failed to delete reminder {ReminderID} from database: {Error}", reminder.Id, res.Error);
                }
            }
        }
    }
}
