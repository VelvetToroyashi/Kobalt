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
        var buffer = new Memory<byte>(new byte[32]);
        
        var identity = await socket.ReceiveAsync(buffer, CancellationToken.None);

        if (!identity.EndOfMessage || identity.MessageType is not WebSocketMessageType.Text)
        {
            _logger.LogWarning("Invalid identity message (End of message: {EndOfMessage}, Message type: {MessageType})", identity.EndOfMessage, identity.MessageType);
            // Client should immediately send OP3, which should fit in a single 32B buffer.
            await socket.CloseAsync(WebSocketCloseStatus.InvalidPayloadData, "Invalid payload", CancellationToken.None);
            return;
        }
        
        if ((buffer.Span[0] ^ 0x30) is not (byte)ShardServerOpcode.Identify)
        {
            _logger.LogWarning("Expected OP2, got OP{Opcode}", buffer.Span[0] ^ 0x30);
            await socket.CloseAsync(WebSocketCloseStatus.InvalidPayloadData, "Invalid opcode", CancellationToken.None);
            return;
        }

        var sessionResult = await _sessionManager.GetClientSessionAsync(headers, ct);
        
        if (!sessionResult.IsDefined(out var session))
        {
            _logger.LogWarning("Failed to get client session: {Error}", sessionResult.Error);
            await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Invalid session", CancellationToken.None);
            return;
        }

        var connection = new WebsocketWatchdog(socket, ct, this);

        // Should *technically* use an indexer, but this is fine.
        // Relying on implementation details. Fun.
        _connections.Add(session.SessionID, connection);

        _logger.LogDebug("Client connection established (Shard {Shard})", session.ShardID);
        connection.Start();

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
                foreach (var sessionID in bucket)
                {
                    if (!_connections.TryGetValue(sessionID, out var connection))
                    {
                        _logger.LogError("Missing session in bucket {BucketID}", i);
                        continue;
                    }

                    await connection.SendAsync(ShardServerOpcode.Ready);

                    _connections.Remove(sessionID);
                }
            }
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

        public async Task<Result> SendAsync(ShardServerOpcode opcode, object? data = null)
        {
            if (_socket.State != WebSocketState.Open)
            {
                return new InvalidOperationError("Socket closed unexpectedly. Bug?");
            }
            
            byte[] json = JsonSerializer.SerializeToUtf8Bytes( new { op = (int)opcode, d = data });
            
            await _socket.SendAsync(json, WebSocketMessageType.Text, true, default);
            
            return Result.FromSuccess();
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
