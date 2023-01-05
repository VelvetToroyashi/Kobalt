using Kobalt.Infrastructure.Types;
using Kobalt.ShardCoordinator.Types;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Extensions.Options;
using Remora.Discord.Caching.Services;
using Remora.Results;

namespace Kobalt.ShardCoordinator.Services;

public class SessionManager
{
    private readonly IOptions<KobaltConfig> _config;
    private readonly CacheService _cacheService;
    private readonly ILogger<SessionManager> _logger;
    private readonly Dictionary<Guid, ClientSession> _sessions = new();
    private readonly ReaderWriterLockSlim _sessionsLock = new(LockRecursionPolicy.SupportsRecursion);
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

    public async Task<Result<ClientSession>> GetClientSessionAsync(RequestHeaders headers, CancellationToken ct)
    {
        _sessionsLock.EnterReadLock();
        var sessionID = headers.Get<Guid>("X-Session-ID");
        if (sessionID == default)
        {
            _sessionsLock.ExitReadLock();
            return new NotFoundError("No session ID was provided.");
        }
        
        if (!_sessions.TryGetValue(sessionID, out var session))
        {
            _sessionsLock.ExitReadLock();
            return new NotFoundError("The session ID provided was not found.");
        }
        
        _sessionsLock.ExitReadLock();

        return session;
    }

    public bool IsValidSession(string? sessionID, int? shardID, out Guid sessionGuid)
    {
        if (!Guid.TryParse(sessionID, out sessionGuid))
        {
            return false;
        }
        
        _sessionsLock.EnterReadLock();
        try
        {
            if (!_sessions.TryGetValue(sessionGuid, out var session))
            {
            
                return false;
            }
        
            var isCorrectShard = session.ShardID == shardID;

            return isCorrectShard;
        }
        finally
        {
            _sessionsLock.ExitReadLock();
        }
    }

    public async Task<Result<ClientSession>> GetNextAvailableSessionAsync()
    {
        _sessionsLock.EnterReadLock();
        
        if (_sessions.Count >= _config.Value.Discord.ShardCount)
        {
            _sessionsLock.ExitReadLock();
            return new InvalidOperationError("No sessions available.");
        }
        
        _sessionsLock.ExitReadLock();
        
        _sessionsLock.EnterWriteLock();
        
        var guid = Guid.NewGuid();
        var session = new ClientSession(guid, GetNextAvailableShardID(), null, null, DateTimeOffset.UtcNow, null);
        
        _sessions.Add(guid, session);
        
        _sessionsLock.ExitWriteLock();
        
        return session;
    }
    
    public async Task<Result> UpdateSessionAsync(Guid sessionID, Func<ClientSession, ClientSession> updateFunc)
    {
        _sessionsLock.EnterWriteLock();
        
        if (!_sessions.TryGetValue(sessionID, out var existingSession))
        {
            _sessionsLock.ExitWriteLock();
            return new NotFoundError("The session ID provided was not found.");
        }
        
        _sessions[sessionID] = updateFunc(existingSession) with { LastSavedAt = DateTimeOffset.UtcNow };
        _sessionsLock.ExitWriteLock();
        
        return Result.FromSuccess();
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
        var ids = Enumerable.Range(0, _config.Value.Discord.ShardCount);

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
            try
            {
                var sessions = _sessions.Values.Where(EligibleForCleanup);
                foreach (var session in sessions)
                {
                    _sessions.Remove(session.SessionID);
                    _logger.LogDebug("Removed abandoned session {SessionID}", session.SessionID);
                }
            }
            finally
            {
                _sessionsLock.ExitWriteLock();
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
