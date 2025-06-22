using System.Collections.Concurrent;
using Kobalt.Bot.Data.DTOs;
using Kobalt.Bot.Data.MediatR.Guilds;
using MediatR;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;

namespace Kobalt.Bot.Services;

public class AntiRaidV2Service
{
    private readonly IUser _self;
    private readonly IMediator _mediator;
    private readonly TimeProvider _timeProvider;
    private readonly InfractionAPIService _infractions;
    private readonly ILogger<AntiRaidV2Service> _logger;
    private readonly ConcurrentDictionary<Snowflake, RaidState> _raidStates = new();

    public AntiRaidV2Service
    (
        IUser self,
        IMediator mediator,
        InfractionAPIService infractions,
        TimeProvider timeProvider,
        ILogger<AntiRaidV2Service> logger
    )
    {
        _self = self;
        _mediator = mediator;
        _infractions = infractions;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>
    /// Handles incoming joins, and and begins tracking the user in the event of a raid.
    /// </summary>
    /// <param name="member">The member that joined.</param>
    public async Task<Result> HandleAsync(IGuildMemberAdd member)
    {
        var configResult = await _mediator.Send(new GetGuild.AntiRaidConfigRequest(member.GuildID));

        if (!configResult.IsDefined(out var config) || !config.IsEnabled)
        {
            return Result.FromSuccess();
        }

        var state = _raidStates.GetOrAdd(member.GuildID, (_) => new RaidState(_timeProvider));
        state.AddUser(member.User.Value, member.JoinedAt, config);

        if (!state.IsRaid(config))
        {
            return Result.FromSuccess();
        }

        return await HandleRaidAsync(member.GuildID, state, config);
    }

    private async Task<Result> HandleRaidAsync(Snowflake guildID, RaidState state, GuildAntiRaidConfigDTO config)
    {
        var users = state.GetSuspiciousUsers(config).ToArray();

        foreach (var user in users)
        {
            var result = await _infractions.AddUserBanAsync(guildID, user, _self, $"Raid detected ({users.Length} accounts).");

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to ban user {UserIdentify} during raid in guild {GuildID}. Reason: {Error}", user.DiscordTag(), guildID, result.Error.Message);
            }
        }

        state.MarkUsersHandled(users.Select(user => user.ID));

        return Result.FromSuccess();
    }
}

/// <summary>
/// Represents the current state of a raid.
/// </summary>
internal class RaidState(TimeProvider timeProvider)
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    internal readonly List<(IUser User, DateTimeOffset JoinDate, int ThreatScore, bool Handled)> _users = new();

    /// <summary>
    /// Checks if a given user's ID is being tracked.
    /// </summary>
    /// <param name="user">The ID of the user to check for.</param>
    /// <returns>Whether the user is currently being tracked by this raid state.</returns>
    public bool IsTrackedUser(Snowflake user)
    {
        _lock.Wait();
        try
        {
            return _users.Any(u => u.User.ID == user);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Adds a user to the tracking state.
    /// </summary>
    /// <param name="user">The user to track.</param>
    /// <param name="joinTimestamp">The timestamp at which they joined.</param>
    /// <param name="config">A configuration to determine how their threat score should be calculated.</param>
    public void AddUser(IUser user, DateTimeOffset joinTimestamp, GuildAntiRaidConfigDTO config)
    {
        _lock.Wait();
        try
        {
            // Check again inside lock
            if (_users.Any(u => u.User.ID == user.ID))
            {
                return;
            }

            var threatScore = config.BaseJoinScore;
            TimeSpan lastJoinDelta = TimeSpan.MaxValue; // Initialize to a large value

            if (_users.Any())
            {
                lastJoinDelta = joinTimestamp - _users.Last().JoinDate;
            }

            var accountAge = user.ID.Timestamp - joinTimestamp;

        if (lastJoinDelta < config.LastJoinBufferPeriod)
        {
            threatScore += config.JoinVelocityScore;
        }

        if (accountAge < config.MinimumAccountAge)
        {
            threatScore += config.MinimumAgeScore;
        }

        if (user.Avatar is null)
        {
            threatScore += config.NoAvatarScore;
        }

        if (accountAge > config.MinimumAccountAgeBypass)
        {
            threatScore = config.BaseJoinScore;
        }

        if (user.Flags.AsNullable() is {} userFlags && config.AccountFlagsBypass is {} bypassFlags)
        {
            // Bypass applies if the user possesses ALL of the flags specified in AccountFlagsBypass.
            // For example, if bypassFlags = (FlagA | FlagB), userFlags must also contain (FlagA | FlagB).
            // If bypass should occur if the user has ANY of the bypassFlags, this condition would be:
            // (userFlags & bypassFlags) != 0
            if ((userFlags & bypassFlags) == userFlags)
            {
                threatScore = config.BaseJoinScore;
            }
        }

        _users.Add((user, joinTimestamp, threatScore, false));
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <returns>Whether or not the current configuration would consider the current state to be a raid.</returns>
    public bool IsRaid(GuildAntiRaidConfigDTO config)
    {
        _lock.Wait();
        try
        {
            ClearState(config);
            var score = _users.Sum(u => u.ThreatScore);
            return score >= config.ThreatScoreThreshold;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Marks users as handled.
    /// </summary>
    /// <param name="users">The users to mark as handled.</param>
    public void MarkUsersHandled(IEnumerable<Snowflake> users)
    {
        _lock.Wait();
        try
        {
            foreach (var userIdToMark in users)
            {
                for (int i = 0; i < _users.Count; i++)
                {
                    if (_users[i].User.ID == userIdToMark)
                    {
                        var userTuple = _users[i];
                        _users[i] = (userTuple.User, userTuple.JoinDate, userTuple.ThreatScore, true); // Mark as handled
                        break;
                    }
                }
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Gets users that are considered suspicious.
    /// </summary>
    /// <param name="config">A config to determine which users should be considered suspicious.</param>
    /// <returns>Suspicious users, as determined by the config.</returns>
    public IEnumerable<IUser> GetSuspiciousUsers(GuildAntiRaidConfigDTO config)
    {
        _lock.Wait();
        try
        {
            ClearState(config);
            // Note: This returns an IEnumerable that might be evaluated lazily.
            // If the caller iterates this outside of a lock, and _users changes, it could lead to issues.
            // Materializing to a List here ensures snapshot semantics.
            return _users.Where(u => u.ThreatScore > config.BaseJoinScore && !u.Handled).Select(u => u.User).ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Clears the current state based on the cooldown period.
    /// </summary>
    /// <param name="config"></param>
    private void ClearState(GuildAntiRaidConfigDTO config)
    {
        var now = timeProvider.GetUtcNow();
        var cutoff = now - config.AntiRaidCooldownPeriod;

        if (_users.LastOrDefault().JoinDate < cutoff)
        {
            _users.Clear();
            return;
        }

        _users.RemoveAll(u => u.JoinDate < cutoff);
    }
}
