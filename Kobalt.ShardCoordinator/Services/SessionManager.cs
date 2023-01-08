using System.Collections.Concurrent;
using System.Diagnostics;
using Kobalt.Infrastructure.Types;
using Kobalt.ShardCoordinator.Types;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Extensions.Options;
using Remora.Discord.Caching.Services;
using Remora.Results;

namespace Kobalt.ShardCoordinator.Services;

/// <summary>
/// Service for handling sessions.
/// </summary>
/// <remarks>
/// Sessions in this context refer to a connection between the server and a client,
/// which also hold metadata about the client.
///
/// <para>
/// This metadata includes:
/// <ul>The shard ID of the client</ul>
/// <ul>The ID of the session to hold a mapping with</ul>
/// <ul>The gateway session ID</ul>
/// <ul>The gateway sequence number</ul>
/// </para>
/// </remarks>
public class SessionManager
{
    private readonly IOptions<KobaltConfig> _config;
    private readonly CacheService _cacheService;
    private readonly ILogger<SessionManager> _logger;
    private readonly ConcurrentDictionary<Guid, ClientSession> _sessions = new();
    private readonly PeriodicTimer _cleanupTimer;
    private readonly Task _cleanupTask;

    public SessionManager(IOptions<KobaltConfig> config, CacheService cacheService, ILogger<SessionManager> logger)
    {
        _config = config;
        _cacheService = cacheService;
        _logger = logger;
        
        _cleanupTimer = new(TimeSpan.FromSeconds(20));

        _cleanupTask = CleanupAbandonedSessionsAsync();
    }

    /// <summary>
    /// Retrieves the session for the given ID.
    /// </summary>
    /// <param name="headers">The headers from the request.</param>
    /// <param name="ct">A cancellation token to abort the request.</param>
    /// <returns>A result containing the client session, if it exists.</returns>
    public async Task<Result<ClientSession>> GetClientSessionAsync(RequestHeaders headers, CancellationToken ct)
    {
        var sessionID = headers.Get<Guid>("X-Session-ID");
        if (sessionID == default)
        {
            return new NotFoundError("No session ID was provided.");
        }

        if (!_sessions.TryGetValue(sessionID, out var session))
        {
            return new NotFoundError("The session ID provided was not found.");
        }
            
        return session;
    }

    public bool IsValidSession(string? sessionID, int shardID, out Guid sessionGuid)
    {
        if (!Guid.TryParse(sessionID, out sessionGuid))
        {
            return false;
        }
        
        if (!_sessions.TryGetValue(sessionGuid, out var session))
        {
            return false;
        }
        
        var isCorrectShard = session.ShardID == shardID;

        return isCorrectShard;
    }

    public async Task<Result<ClientSession>> GetNextAvailableSessionAsync()
    {
        if (_sessions.Count >= _config.Value.Discord.ShardCount)
        {
            return new InvalidOperationError("No sessions available.");
        }

        var guid = Guid.NewGuid();
        var session = new ClientSession(guid, GetNextAvailableShardID(), null, null, DateTimeOffset.UtcNow, null);

        _sessions.TryAdd(guid, session);

        return session;
    }
    
    public async Task<Result> UpdateSessionAsync(Guid sessionID, Func<ClientSession, ClientSession> updateFunc)
    {
        if (!_sessions.TryGetValue(sessionID, out var existingSession))
        {
            return new NotFoundError("The session ID provided was not found.");
        }
        
        _sessions[sessionID] = updateFunc(existingSession) with { LastSavedAt = DateTimeOffset.UtcNow };
        
        return Result.FromSuccess();
    }
    
    public void ReleaseSession(Guid sessionID)
    {
        var session = _sessions[sessionID];
        
        _sessions[sessionID] = session with { AbandonedAt = DateTimeOffset.UtcNow };
    }
    
    public void ReclaimSession(Guid sessionID)
    {
        var session = _sessions[sessionID];
        
        _sessions[sessionID] = session with { AbandonedAt = null };
    }
    
    private int GetNextAvailableShardID()
    {
        Debug.Assert(_sessions.Count < _config.Value.Discord.ShardCount);
        
        var ids = Enumerable.Range(0, _config.Value.Discord.ShardCount);

        foreach (var id in ids)
        {
            if (_sessions.Values.Any(v => v.ShardID == id))
            {
                continue;
            }
            
            return id;
        }
        
        // Throwing as we have no data to return in this situation; this case is exceptional.
        throw new InvalidOperationException("No available shard IDs.");
    }
    
    private async Task CleanupAbandonedSessionsAsync()
    {
        while (await _cleanupTimer.WaitForNextTickAsync())
        {
            var sessions = _sessions.Values.Where(EligibleForCleanup);
            foreach (var session in sessions)
            {
                _sessions.Remove(session.SessionID, out _);
                _logger.LogDebug("Removed abandoned session {SessionID}", session.SessionID);
            }
        }
        
        static bool EligibleForCleanup(ClientSession session)
        {
            var isAbandoned = session.AbandonedAt != null;
            var isEligbleForAbandomentCleanup = (session.AbandonedAt + TimeSpan.FromSeconds(20)) < DateTime.UtcNow;
            var isLikelyInvalid = session.LastSavedAt + TimeSpan.FromSeconds(70) < DateTime.UtcNow;
            
            return isEligbleForAbandomentCleanup || (isAbandoned && isLikelyInvalid);
        }
    }
}
