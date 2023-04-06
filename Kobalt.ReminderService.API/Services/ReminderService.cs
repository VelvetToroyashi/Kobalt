using System.Net.WebSockets;
using System.Text.Json;
using Kobalt.Infrastructure.DTOs.Reminders;
using Kobalt.Infrastructure.Extensions.Remora;
using Kobalt.ReminderService.Data.Mediator;
using Mediator;

namespace Kobalt.ReminderService.API.Services;

/// <summary>
/// A service that dispatches reminders;
/// </summary>
public class ReminderService : IHostedService
{
    private readonly IMediator _mediator;
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(1));
    private readonly Dictionary<WebSocket, CancellationTokenSource> _clients = new();
    private readonly List<ReminderDTO> _reminders = new();
    private readonly ILogger<ReminderService> _logger;
    
    private readonly Task _dispatchTask;
    private readonly CancellationTokenSource _cts = new();
    
    public ReminderService(IMediator mediator, ILogger<ReminderService> logger)
    {
        _mediator = mediator;
        _logger = logger;
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
    /// Adds a client to be dispatched to by the reminder service.
    /// </summary>
    /// <param name="client">The client to be added.</param>
    /// <param name="cts">
    /// A cancellation token source. This is cancelled when the client is considered "dead"
    /// and should release the connection to the client, if the client hasn't already.
    /// </param>
    public void AddClient(WebSocket client, CancellationTokenSource cts)
    {
        _clients.Add(client, cts);
    }
    
    /// <summary>
    /// Dispatches expired reminders in a round-robin fashion, requeing reminders that fail to send.
    /// </summary>
    /// <remarks>
    /// Dead clients are removed from the client list, and the corresponding <see cref="CancellationTokenSource"/> is cancelled.
    /// </remarks>
    private async Task DispatchReminders()
    {
        _logger.LogDebug("Loading reminders...");
        var reminders = await _mediator.Send(new GetAllReminders.Request());
        
        _reminders.AddRange(reminders);
        
        while (await _timer.WaitForNextTickAsync(_cts.Token))
        {
            for (int i = _reminders.Count; i >= 0; i--)
            {
                var reminder = _reminders[i];
                
                if (reminder.Expiration <= DateTime.UtcNow)
                {
                    _reminders.RemoveAt(i);
                }

                var payload = JsonSerializer.SerializeToUtf8Bytes(reminder).AsMemory();
                var sent = await TryDispatchReminderAsync(payload, reminder.Id);
                
                if (!sent)
                {
                    _logger.LogWarning("No clients connected to receive reminder {ReminderID}, requeing.", reminder.Id);
                    _reminders.Add(reminder);
                    continue;
                }

                await _mediator.Send(new DeleteReminder.Request(reminder.Id, reminder.AuthorID.Value));
            }
        }
        
        _logger.LogInformation("Shutting down. Releasing all clients.");
        
        foreach (var client in _clients.Keys)
        {
            _ = await ResultExtensions.TryCatchAsync(async () => await client.CloseAsync(WebSocketCloseStatus.EndpointUnavailable, "Server shutting down.", CancellationToken.None));
        }
    }

    /// <summary>
    /// Attempts to dispatch a reminder to the first connected client in a round-robin manner.
    /// </summary>
    /// <param name="payload">The payload to dispatch.</param>
    /// <param name="reminderID">The reminder's ID</param>
    /// <returns>Whether the dispatch succeeded</returns>
    private async Task<bool> TryDispatchReminderAsync(Memory<byte> payload, int reminderID)
    {
        foreach ((var client, var cts) in _clients)
        {
            if (cts.IsCancellationRequested)
            {
                // Client disconnected, eject.
                _clients.Remove(client);
                continue;
            }
            
            var result = await ResultExtensions.TryCatchAsync(async () => await client.SendAsync(payload, WebSocketMessageType.Text, true, CancellationToken.None));

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to send reminder {ReminderID} to client, removing.", reminderID);
                _clients.Remove(client);
                cts.Cancel();
                continue;
            }

            return true;
        }

        return false;
    }
}
