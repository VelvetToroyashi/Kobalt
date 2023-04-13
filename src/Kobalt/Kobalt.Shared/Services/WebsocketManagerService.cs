using System.Collections.Concurrent;
using System.Net.WebSockets;
using Kobalt.Shared.Extensions;
using Microsoft.Extensions.Logging;
using Remora.Results;

namespace Kobalt.Shared.Services;

internal record WebSocketConnection(WebSocket Socket, TaskCompletionSource LifetimeTask, CancellationTokenSource Cancellation);

/// <summary>
/// Represents a service that handles connections to multiple websocket clients.
/// <para>
/// This service offers a unified, thread-safe aggregation for multiple connected websocket clients where the implementation detail of a connection
/// is not particularly important. This is to say, the caller of this service should not rely on the specific state of any one given socket.
///
/// Sockets can be added and removed at any time, and connections can be terminated in a bi-directional manner.
/// (i.e. both the server (the entrypoint into the service that added the client) and the client itself can terminate a connection, removing it from the aggregate.)
/// </para>
/// </summary>
/// <remarks>Currently, the API does not support receving inputs from sockets, and any received messages will be discarded.</remarks>
public class WebsocketManagerService
{
    private readonly AsyncLocal<byte[]> _asyncLocal;
    private readonly ILogger<WebsocketManagerService> _logger;
    private readonly ConcurrentDictionary<Guid, WebSocketConnection> _sockets;
    
    /// <summary>
    /// Creates a new <see cref="WebsocketManagerService"/>.
    /// </summary>
    /// <param name="logger"></param>
    public WebsocketManagerService(ILogger<WebsocketManagerService> logger)
    {
        _logger = logger;
        _sockets = new();
        _asyncLocal = new();
        _asyncLocal.Value = new byte[1024];
    }

    /// <summary>
    /// Adds a client to the service, potentially starting a receive loop.
    /// </summary>
    /// <param name="socket">The socket to hand off.</param>
    /// <param name="intendToRead">
    /// <para>
    /// Whether the caller intends to read from the socket. This defaults to false,
    /// and controls the decision of the service in regards to how to handle the client disconnecting.
    ///
    /// It is considered erroneous to specify this as true, and then not read from the socket, or vice-versa.
    /// In the former case, the service will poll the socket's current state instead of reading from it,
    /// but becuase of the design of websockets, the server will never detect a closure as this requires reading
    /// from the socket to receive the FIN message indicating closure. In this case, the socket will remain "open"
    /// (however in a zombied state) until the server attempts to send data to the client, which will then trigger
    /// the socket to update its state (as the server will fail to send data to a closed socket).
    ///
    /// In the inverse case, it is erroneous because while the socket may be read and written to simultaneously, either
    /// operation is not thread-safe in and of itself. If this is specified to be false (the default), the service will
    /// continuously poll the socket for data (which will thusly read the aforementioned FIN message) and will then
    /// will close the socket, releasing the associated <see cref="TaskCompletionSource"/> which is used to indicate to
    /// the caller that the socket has been closed. This is the recommended approach, as this abstracts away the need to
    /// poll the socket manually, however it does limit the ability to read from the socket without encountering potentially
    /// undefined behavior. If reading from the socket is required, the entire receive loop is the caller's responsibility.
    /// </para>
    /// </param>
    /// <param name="ct">A cancellation token to tie to join with the service's internal <see cref="CancellationTokenSource"/> which is used to
    /// terminate the connection of the websocket.</param>
    /// <returns>The ID of the socket connection.</returns>
    public Guid AddClient(WebSocket socket, bool intendToRead = false, CancellationToken ct = default)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var guid = Guid.NewGuid();
        var tcs = new TaskCompletionSource();
        
        _sockets.TryAdd(guid, new WebSocketConnection(socket, tcs, cts));
        _logger.LogDebug("Added new client with connection ID {ID}.", guid);
        
        _ = ReceiveAsync(guid, intendToRead, socket, cts, tcs);
        
        return guid;
    }
    
    /// <summary>
    /// Attempts to remove a client from the service.
    /// </summary>
    /// <param name="id">The ID of the client to remove.</param>
    /// <returns>An erro if the client doesn't exist, otherwise a successful result.</returns>
    public Result RemoveClient(Guid id)
    {
        if (!_sockets.TryRemove(id, out var connection))
        {
            return new NotFoundError($"No client with connection ID {id} was found.");
        }
        connection.Cancellation.Cancel();
        connection.LifetimeTask.TrySetResult();
        
        _logger.LogDebug("Removed client with connection ID {ID}.", id);
        
        return Result.FromSuccess();
    }
    
    /// <summary>
    /// Waits for a given client to disconnect, or be disconnected by the service.
    /// </summary>
    /// <param name="id">The ID of the client to wait on.</param>
    /// <returns>An error if the specified client does not exist, otherwise a successful result.</returns>
    /// <remarks>If the intent to read was specified, the caller is expected to read
    /// from the client instead of using this method. See <see cref="AddClient"/> for more information.</remarks>
    public async ValueTask<Result> WaitForDisconnectAsync(Guid id)
    {
        if (!_sockets.TryGetValue(id, out var connection))
        {
            return new NotFoundError($"No client with connection ID {id} was found.");
        }

        await connection.LifetimeTask.Task;
        
        return Result.FromSuccess();
    }

    /// <summary>
    /// Broadcasts a message to all connected clients indiscriminately.
    /// </summary>
    /// <param name="data">The data to broadcast to clients.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>A result that may or not have succeeded.</returns>
    public async ValueTask<Result> BroadcastAsync(Memory<byte> data, CancellationToken ct = default)
    {
        if (_sockets.Count is 0)
        {
            // Should this be considered successful?
            return new InvalidOperationError("No clients are connected.");
        }

        return await SendAsync(data, false, ct);
    }

    /// <summary>
    /// Sends a message to the first available client.
    /// </summary>
    /// <param name="data">The data to send.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>A result that may or not have succeeded.</returns>
    public async Task<Result> SendAsync(Memory<byte> data, CancellationToken ct = default)
    {
        if (_sockets.Count is 0)
        {
            // Should this be considered successful?
            return new InvalidOperationError("No clients are connected.");
        }

        return await SendAsync(data, true, ct);
    }


    /// <summary>
    /// Broadcasts a message either to all connected clients, or the first client that successfully receives the message.
    /// </summary>
    /// <param name="data">The data to send.</param>
    /// <param name="stopOnFirstSuccess">Whether to stop on the first successful message.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    private async Task<Result> SendAsync(Memory<byte> data, bool stopOnFirstSuccess, CancellationToken ct = default)
    {
        foreach ((var id, var conn) in _sockets)
        {
            var result = await ResultExtensions.TryCatchAsync(() => conn.Socket.SendAsync(new ReadOnlyMemory<byte>(data.ToArray()), WebSocketMessageType.Text, true, ct).AsTask());

            if (!result.IsSuccess)
            {
                _logger.LogError("Failed to send message to client with connection ID {ID}: {Error}", id, result.Error);
                RemoveClient(id);
            }
            else
            {
                if (stopOnFirstSuccess)
                {
                    return Result.FromSuccess();
                }
            }
        }
        
        return Result.FromSuccess();
    }

    /// <summary>
    /// Receives input from a given socket, cancelling the associated CancellationTokenSource if the socket is disconnected.
    /// </summary>
    private async Task ReceiveAsync(Guid id, bool intendToRead, WebSocket socket, CancellationTokenSource cts, TaskCompletionSource tcs)
    {
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(500));
        
        // Assume the caller wants to read from the socket, and will read the disconnect.
        if (intendToRead)
        {
            while (await timer.WaitForNextTickAsync(cts.Token))
            {
                if (socket.State is not WebSocketState.Open)
                {
                    break;
                }
            }

            tcs.TrySetResult();
            _sockets.TryRemove(id, out _);

            _logger.LogDebug("Client with connection ID {ID} disconnected, or the server requested a disconnect.", id);
            return;
        }

        while (!cts.IsCancellationRequested)
        {
            var result = await ResultExtensions.TryCatchAsync(() => socket.ReceiveAsync(_asyncLocal.Value!, cts.Token));
            if (!result.IsDefined(out var socketMessage) || socketMessage.MessageType is WebSocketMessageType.Close)
            {
                tcs.TrySetResult();
                _sockets.TryRemove(id, out _);
                return;
            }
        }
    }
}
