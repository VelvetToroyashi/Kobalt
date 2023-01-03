using Kobalt.ShardCoordinator.Types;
using Microsoft.AspNetCore.Http.Headers;
using Remora.Discord.Caching.Services;
using Remora.Results;

namespace Kobalt.ShardCoordinator.Services;

public class SessionManager
{
    private readonly int _maxShards;
    private readonly CacheService _cacheService;
    private readonly ILogger<SessionManager> _logger;
    private readonly Dictionary<Guid, ClientSession> _sessions = new();
    private readonly ReaderWriterLockSlim _sessionsLock = new(LockRecursionPolicy.NoRecursion);
    private readonly PeriodicTimer _cleanupTimer;
    
    public SessionManager(CacheService cacheService, ILogger<SessionManager> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
        
        _cleanupTimer = new(TimeSpan.FromSeconds(20));
    }

    public async Task<Result<ClientSession>> GetClientSessionAsync(RequestHeaders headers, CancellationToken ct)
    {
        _sessionsLock.EnterReadLock();
        if (headers.Get<Guid?>("X-Session-ID") is not {} sessionID)
        {
            return new NotFoundError("No session ID was provided.");
        }
        
        if (!_sessions.TryGetValue(sessionID, out var session))
        {
            return new NotFoundError("The session ID provided was not found.");
        }
        
        _sessionsLock.ExitReadLock();

        return session;
    }

    public async Task<Result<ClientSession>> GetNextAvailableSessionAsync()
    {
        _sessionsLock.EnterReadLock();
        
        if (_sessions.Count == _maxShards)
        {
            return new InvalidOperationError("No sessions available.");
        }
        
        _sessionsLock.ExitReadLock();
        
        _sessionsLock.EnterWriteLock();
        
        var guid = Guid.NewGuid();
        var session = new ClientSession(guid, GetNextAvailableShardID(), null, null, null, null);
        
        _sessions.Add(guid, session);
        
        _sessionsLock.ExitWriteLock();
        
        return session;
    }
    
    public void AbandonSession(Guid sessionID)
    {
        _sessionsLock.EnterWriteLock();
        
        var session = _sessions[sessionID];
        
        _sessions[sessionID] = session with { AbandonedAt = DateTimeOffset.UtcNow };
        
        _sessionsLock.ExitWriteLock();
    }
    
    public void ReclaimSession(Guid sessionID)
    {
        _sessionsLock.EnterWriteLock();
        
        var session = _sessions[sessionID];
        
        _sessions[sessionID] = session with { AbandonedAt = null };
        
        _sessionsLock.ExitWriteLock();
    }
    
    private int GetNextAvailableShardID()
    {
        var ids = Enumerable.Range(0, _maxShards);

        foreach (var id in ids)
        {
            if (_sessions.Values.Any(v => v.ShardID == id))
            {
                continue;
            }
            
            return id;
        }
        
        throw new ArgumentOutOfRangeException("No shards available.");
    }
    
    private async Task CleanupAbandonedSessionsAsync()
    {
        while (await _cleanupTimer.WaitForNextTickAsync())
        {
            _sessionsLock.EnterWriteLock();
        
            var sessions = _sessions.Values.Where(x => x.AbandonedAt < DateTime.UtcNow - TimeSpan.FromSeconds(30)).ToArray();
            foreach (var session in sessions)
            {
                _sessions.Remove(session.SessionID);
                _logger.LogDebug("Removed abandoned session {SessionID}", session.SessionID);
            }

            _sessionsLock.ExitWriteLock();
        }
    }
}
