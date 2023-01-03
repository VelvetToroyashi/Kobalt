using System.Globalization;
using System.Net.WebSockets;
using Kobalt.Infrastructure.Enums;
using Kobalt.ShardCoordinator.Types;
using Microsoft.AspNetCore.Http.Headers;
using Remora.Discord.Caching.Services;
using Remora.Results;

namespace Kobalt.ShardCoordinator.Services;

internal class WebsocketWatchdog : IDisposable
{
    private readonly WebSocket _socket;
    private readonly TaskCompletionSource _tcs;
    private readonly CancellationToken _cancellationToken;
    private readonly WebsocketManagerService _manager;
    private readonly PeriodicTimer _heartbeatTimer;
    
    public Task WebsocketTask => _tcs.Task;

    private Task _receiveTask;
    
    public WebsocketWatchdog
    (
        WebSocket socket,
        CancellationToken ct,
        WebsocketManagerService manager
    )
    {
        _socket = socket;
        _tcs = new(false);
        _cancellationToken = ct;
        _manager = manager;
        
        ct.Register(() => Dispose());
        
        _heartbeatTimer = new(TimeSpan.FromSeconds(1));
    }

    public void Dispose()
    {
        _tcs.TrySetResult();
        _socket.Dispose();
        _heartbeatTimer.Dispose();
    }
}

public class WebsocketManagerService
{
    private const string SessionKey = "ksc:sessions:{0}";
    
    
    private readonly int _maxShards;
    private readonly List<WebsocketWatchdog> _connections;
    private readonly CacheService _cache;
    private readonly SessionManager _sessionManager;

    public async Task HandleConnectionAsync(WebSocket socket, RequestHeaders headers, CancellationToken ct)
    {
        var buffer = new Memory<byte>(new byte[32]);
        
        var identity = await socket.ReceiveAsync(buffer, CancellationToken.None);

        if (!identity.EndOfMessage || identity.MessageType is not WebSocketMessageType.Text)
        {
            // Client should immediately send OP3, which should fit in a single 32B buffer.
            await socket.CloseAsync(WebSocketCloseStatus.InvalidPayloadData, "Invalid payload", CancellationToken.None);
            return;
        }

        if (buffer.Span[0] is not (byte)ShardServerOpcodes.Identify)
        {
            await socket.CloseAsync(WebSocketCloseStatus.InvalidPayloadData, "Invalid opcode", CancellationToken.None);
            return;
        }

        var sessionResult = await _sessionManager.GetClientSessionAsync(headers, ct);
        
        if (!sessionResult.IsSuccess)
        {
            await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Invalid session", CancellationToken.None);
            return;
        }

        var connection = new WebsocketWatchdog(socket, ct, this);

        _connections.Add(connection);

        await connection.WebsocketTask;
    }
}
