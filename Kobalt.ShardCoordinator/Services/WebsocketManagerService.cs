using System.Globalization;
using System.Net.WebSockets;
using Kobalt.Infrastructure.Enums;
using Kobalt.Infrastructure.Types;
using Kobalt.ShardCoordinator.Types;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Extensions.Options;
using Remora.Discord.Caching.Services;
using Remora.Results;

namespace Kobalt.ShardCoordinator.Services;


public class WebsocketManagerService
{
    private const string SessionKey = "ksc:sessions:{0}";

    private readonly IOptions<KobaltConfig> _config;
    private readonly SessionManager _sessionManager;
    private readonly ILogger<WebsocketManagerService> _logger;

    private readonly PeriodicTimer _timer;
    private readonly Dictionary<int, List<Guid>> _ratelimitBuckets;
    private readonly Dictionary<Guid, WebsocketWatchdog> _connections = new();
    
    public WebsocketManagerService(IOptions<KobaltConfig> config, SessionManager sessionManager, ILogger<WebsocketManagerService> logger)
    {
        _config = config;
        _sessionManager = sessionManager;
        _timer = new(TimeSpan.FromSeconds(6));
        _logger = logger;
        _ratelimitBuckets = new();
    }

    public async Task HandleConnectionAsync(WebSocket socket, RequestHeaders headers, CancellationToken ct)
    {
        _logger.LogDebug("Handling new client connection");
        var buffer = new Memory<byte>(new byte[32]);
        
        var identity = await socket.ReceiveAsync(buffer, CancellationToken.None);

        if (!identity.EndOfMessage || identity.MessageType is not WebSocketMessageType.Text)
        {
            _logger.LogWarning("Invalid identity message (End of message: {EndOfMessage}, Message type: {MessageType})", identity.EndOfMessage, identity.MessageType);
            // Client should immediately send OP3, which should fit in a single 32B buffer.
            await socket.CloseAsync(WebSocketCloseStatus.InvalidPayloadData, "Invalid payload", CancellationToken.None);
            return;
        }
        
        if ((buffer.Span[0] ^ 0x30) is not (byte)ShardServerOpcodes.Identify)
        {
            _logger.LogWarning("Expected OP2, got OP{Opcode}", buffer.Span[0] ^ 0x30);
            await socket.CloseAsync(WebSocketCloseStatus.InvalidPayloadData, "Invalid opcode", CancellationToken.None);
            return;
        }

        var sessionResult = await _sessionManager.GetClientSessionAsync(headers, ct);
        
        if (!sessionResult.IsSuccess)
        {
            _logger.LogWarning("Failed to get client session: {Error}", sessionResult.Error);
            await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Invalid session", CancellationToken.None);
            return;
        }

        var connection = new WebsocketWatchdog(socket, ct, this);

        // Should *technically* use an indexer, but this is fine.
        // Relying on implementation details. Fun.
        _connections.Add(sessionResult.Entity.SessionID, connection);

        _logger.LogDebug("Client connection established");
        connection.Start();
        await connection.WebsocketTask;
    }

    public async Task<Result> TerminateClientSessionAsync(Guid sessionID, string gatewaySessionID, int sequence)
    {
        if (!_connections.TryGetValue(sessionID, out var connection))
        {
            return new NotFoundError();
        }
        
        //Dispose will close the socket and release the middleware.
        connection.Dispose();
        
        //Remove the connection from the dictionary.
        _connections.Remove(sessionID);
        
        var updateResult = await _sessionManager.UpdateSessionAsync
        (
            sessionID, 
            session => session with { GatewaySessionID = gatewaySessionID, Sequence = sequence }
        );

        return updateResult;
    }

    private class WebsocketWatchdog : IDisposable
    {
        private readonly WebSocket _socket;
        private readonly TaskCompletionSource _tcs;
        private readonly CancellationTokenSource _cts;
        private readonly CancellationToken _cancellationToken;
        private readonly WebsocketManagerService _manager;
        private readonly PeriodicTimer _heartbeatTimer;
    
        public Task WebsocketTask => _tcs.Task;

        private Task _watchdogTask;
    
        public WebsocketWatchdog
        (
            WebSocket socket,
            CancellationToken ct,
            WebsocketManagerService manager
        )
        {
            _socket = socket;
            _tcs = new(false);
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _cancellationToken = _cts.Token;
            _manager = manager;
        
            // Register on the parent cancellation token; disposing will our token.
            ct.Register(() => Dispose());
        
            _heartbeatTimer = new(TimeSpan.FromSeconds(1));
        }
        
        public void Start()
        {
            _watchdogTask = WatchdogAsync();
        }

        private async Task WatchdogAsync()
        {
            while (await _heartbeatTimer.WaitForNextTickAsync(_cancellationToken))
            {
                if (_socket.State is not WebSocketState.Open)
                {
                    await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", CancellationToken.None);
                    _tcs.TrySetResult();
                    return;
                }
            }
        }

        public void Dispose()
        {
            _tcs.TrySetResult();
            _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, default);
            _socket.Dispose();
            _heartbeatTimer.Dispose();
        }
    }
}
