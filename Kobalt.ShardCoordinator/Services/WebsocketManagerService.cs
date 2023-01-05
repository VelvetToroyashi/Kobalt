using System.Buffers;
using System.Globalization;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Kobalt.Infrastructure.Enums;
using Kobalt.Infrastructure.Types;
using Kobalt.ShardCoordinator.Types;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Extensions.Options;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Caching.Services;
using Remora.Results;

namespace Kobalt.ShardCoordinator.Services;


public class WebsocketManagerService
{
    private readonly SessionManager _sessionManager;
    private readonly IDiscordRestGatewayAPI _gatewayApi;
    private readonly IOptions<KobaltConfig> _config;
    private readonly ILogger<WebsocketManagerService> _logger;

    private readonly Task _bucketSweepTask;
    private readonly PeriodicTimer _timer;
    private readonly Dictionary<int, List<Guid>> _ratelimitBuckets;
    private readonly Dictionary<Guid, WebsocketWatchdog> _connections = new();

    private static readonly byte[] _helloPayload = JsonSerializer.SerializeToUtf8Bytes(new ShardServerPayload(ShardServerOpcode.Hello, null));
    
    private int _maxConcurrency;
    
    public WebsocketManagerService(SessionManager sessionManager, IDiscordRestGatewayAPI gatewayApi, IOptions<KobaltConfig> config, ILogger<WebsocketManagerService> logger)
    {
        _sessionManager = sessionManager;
        _gatewayApi = gatewayApi;
        _config = config;
        _logger = logger;
        
        _timer = new(TimeSpan.FromSeconds(6));
        _logger = logger;
        _ratelimitBuckets = new();

        _bucketSweepTask = StartBucketsAsync();
    }
    
    public async Task HandleConnectionAsync(WebSocket socket, RequestHeaders headers, CancellationToken ct)
    {
        _logger.LogDebug("Handling new client connection");
        
        await socket.SendAsync(_helloPayload, WebSocketMessageType.Text, true, ct);

        var buffer = new Memory<byte>(new byte[32]);
        var identifyMessage = await socket.ReceiveAsync(buffer, CancellationToken.None);
        
        if (!identifyMessage.EndOfMessage || identifyMessage.MessageType is not WebSocketMessageType.Text)
        {
            _logger.LogWarning("First message from the client invalid. Disconnecting.");
            // Client should immediately send OP3, which should fit in a single 32B buffer.
            await socket.CloseAsync(WebSocketCloseStatus.InvalidPayloadData, "Invalid payload", CancellationToken.None);
            return;
        }
        
        var deserialized = JsonSerializer.Deserialize<ShardServerPayload>(buffer.Span[..identifyMessage.Count]);

        if (deserialized.Opcode is not ShardServerOpcode.Identify)
        {
            _logger.LogWarning("Expected client to identify. Got {Opcode} instead. Disconnecting.", deserialized.Opcode);
            await socket.CloseAsync(WebSocketCloseStatus.InvalidPayloadData, "Expected OP3", CancellationToken.None);
            return;
        }

        var sessionResult = await _sessionManager.GetClientSessionAsync(headers, ct);
        
        if (!sessionResult.IsDefined(out var session))
        {
            _logger.LogWarning("Failed to get client session: {Error}", sessionResult.Error);
            await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Invalid session", CancellationToken.None);
            return;
        }

        var connection = new WebsocketWatchdog(session.SessionID, socket, ct, this);

        // Should *technically* use an indexer, but this is fine.
        // Relying on implementation details. Fun.
        _connections.TryAdd(session.SessionID, connection);
        _sessionManager.ReclaimSession(session.SessionID);

        _logger.LogDebug("Client connection established (Shard {Shard})", session.ShardID);

        await ConfigureStartBucketsAsync();
        
        AssignShardToBucket(session.ShardID, session.SessionID);
        
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

    private async Task StartBucketsAsync()
    {
        // This timer ticks every 6 seconds, so 
        // it's okay to start all shards in a given
        // bucket at once.
        while (await _timer.WaitForNextTickAsync())
        {
            for (int i = 0; i < _maxConcurrency; i++)
            {
                if (!_ratelimitBuckets.TryGetValue(i, out var bucket))
                {
                    _logger.LogError("Expected bucket {Bucket} to exist", i);
                    continue;
                }

                // TODO: Limit this to 8 shards per burst, regardless of bucket size
                for (var j = bucket.Count - 1; j >= 0; j--)
                {
                    var sessionID = bucket[j];
                    if (!_connections.TryGetValue(sessionID, out var connection))
                    {
                        _logger.LogError("Missing session in bucket {BucketID}", j);
                        continue;
                    }

                    await connection.SendAsync(ShardServerOpcode.Ready);

                    bucket.Remove(sessionID);
                }
            }
        }
    }

    private async ValueTask HandleSocketMessageAsync(WebsocketWatchdog watchdog, Memory<byte> data, bool isClosing)
    {
        // Currently all this method does is hadnle close messages, as there's
        // nothing the client currently sends other than IDENTIFY (OP3)

        if (isClosing)
        {
            _connections.Remove(watchdog.SessionID);
            _sessionManager.AbandonSession(watchdog.SessionID);
            watchdog.Dispose();
            return;
        }
    }
    
    /// <summary>
    /// Sets the <see cref="_maxConcurrency"/> to the maximum number of shards that can be connected to the gateway at once.
    /// </summary>
    private async ValueTask ConfigureStartBucketsAsync()
    {
        if (_maxConcurrency != 0)
        {
            return;
        }
        
        var gatewayResult = await _gatewayApi.GetGatewayBotAsync();
        
        if (!gatewayResult.IsDefined(out var gatewayInformation))
        {
            _logger.LogError("Failed to get gateway info from Discord: {Error}", gatewayResult.Error);
            return;
        }
        
        var maxConcurrency = gatewayInformation.SessionStartLimit.Value.MaxConcurrency;
        
        _logger.LogInformation("Max concurrency is {MaxConcurrency}", maxConcurrency);
        
        _maxConcurrency = maxConcurrency;
        
        var shardsPerBucket = (int)Math.Ceiling((double)_config.Value.Discord.ShardCount / maxConcurrency);
        for (var i = 0; i < maxConcurrency; i++)
        {
            _ratelimitBuckets.Add(i, new(shardsPerBucket));
        }
    }

    /// <summary>
    /// Assigns a shard (identified by its ID) to a bucket.
    /// </summary>
    /// <param name="shardId">The ID of the shard to assign.</param>
    /// <param name="sessionId">The ID of the shard's session.</param>
    /// <remarks>Discord docs: https://discord.dev/topics/gateway#sharding-max-concurrency</remarks>
    private void AssignShardToBucket(int shardId, Guid sessionId)
    {
        var bucketId = shardId % _maxConcurrency / _maxConcurrency;
        _ratelimitBuckets[bucketId].Add(sessionId);
    }

    private class WebsocketWatchdog : IDisposable
    {
        private readonly WebSocket _socket;
        private readonly TaskCompletionSource _tcs;
        private readonly CancellationTokenSource _cts;
        private readonly CancellationToken _cancellationToken;
        private readonly WebsocketManagerService _manager;
        private readonly PeriodicTimer _heartbeatTimer;

        private bool _isDisposed;
        private Task _watchdogTask;
        
        public Guid SessionID { get; }
        public Task WebsocketTask => _tcs.Task;
        
        public WebsocketWatchdog
        (
            Guid sessionID,
            WebSocket socket,
            CancellationToken ct,
            WebsocketManagerService manager
        )
        {
            SessionID = sessionID;
            
            _socket = socket;
            _tcs = new(false);
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _cancellationToken = _cts.Token;
            _manager = manager;
        
            // Register on the parent cancellation token;
            // disposing will our token.
            ct.Register(() => Dispose());
        
            _heartbeatTimer = new(TimeSpan.FromSeconds(1));
            
            _watchdogTask = WatchdogAsync();
        }

        public async Task<Result> SendAsync(ShardServerOpcode opcode, object? data = null)
        {
            if (_socket.State != WebSocketState.Open)
            {
                return new InvalidOperationError("Socket closed unexpectedly. Bug?");
            }
            
            byte[] json = JsonSerializer.SerializeToUtf8Bytes(new { op = (int)opcode, d = data });
            
            await _socket.SendAsync(json, WebSocketMessageType.Text, true, default);
            
            return Result.FromSuccess();
        }

        private async Task WatchdogAsync()
        {
            var buffer = ArrayPool<byte>.Shared.Rent(4096);
            try
            {
                while (!_cancellationToken.IsCancellationRequested)
                {
                    // Normally you do NOT want to pass a CT to ReceiveAsync
                    // because it registers to abort the socket if the CT is cancelled.
                    // However, here it's fine because if the CT is aborted, 
                    // the client has already disconnected anyway, or the server is shutting down.
                    var res = await _socket.ReceiveAsync(buffer, _cancellationToken);

                    if (res.MessageType == WebSocketMessageType.Close)
                    {
                        // Client has disconnected unexpectedly! Server-initiated disconnects
                        // will dispose, which cancels, and thusly we'll never reach this point!
                        await _manager.HandleSocketMessageAsync(this, buffer.AsMemory(0, res.Count), true);
                        return;
                    }
                }
            }
            catch { /* Socket was aborted, do nothing. */ }
            finally { ArrayPool<byte>.Shared.Return(buffer); }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _cts.Dispose();
                _tcs.TrySetResult();
                _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, default);
                _socket.Dispose();
                _heartbeatTimer.Dispose();
                _isDisposed = true;
            }
        }
    }
}
