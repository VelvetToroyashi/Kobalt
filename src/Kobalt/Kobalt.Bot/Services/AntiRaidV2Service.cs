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
    private readonly InfractionAPIService _infractions;
    private readonly ConcurrentDictionary<Snowflake, RaidState> _raidStates = new();

    public AntiRaidV2Service(IUser self, IMediator mediator, InfractionAPIService infractions)
    {
        _self = self;
        _mediator = mediator;
        _infractions = infractions;
    }

    /// <summary>
    /// Handles incoming joins, and and begins tracking the user in the event of a raid.
    /// </summary>
    /// <param name="member">The member that joined.</param>
    public async Task<Result> HandleAsync(IGuildMemberAdd member)
    {
        var state = _raidStates.GetOrAdd(member.GuildID, (_) => new RaidState());
        var configResult = await _mediator.Send(new GetGuild.AntiRaidConfigRequest(member.GuildID));

        if (!configResult.IsDefined(out var config) || !config.IsEnabled)
        {
            return Result.FromSuccess();
        }

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
                // TODO: Log
            }
        }

        state.MarkUsersHandled(users.Select(user => user.ID));

        return Result.FromSuccess();
    }
}

/// <summary>
/// Represents the current state of a raid.
/// </summary>
internal class RaidState
{
    internal readonly List<(IUser User, DateTimeOffset JoinDate, int ThreatScore, bool Handled)> _users = new();

    /// <summary>
    /// Adds a user to the tracking state.
    /// </summary>
    /// <param name="user">The user to track.</param>
    /// <param name="joinTimestamp">The timestamp at which they joined.</param>
    /// <param name="config">A configuration to determine how their threat score should be calculated.</param>
    public void AddUser(IUser user, DateTimeOffset joinTimestamp, GuildAntiRaidConfigDTO config)
    {
        var threatScore = config.BaseJoinScore;
        var lastJoinDelta = joinTimestamp - _users.LastOrDefault().Item2;
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

        if (accountAge > config.MiniumAccountAgeBypass)
        {
            threatScore = config.BaseJoinScore;
        }

        if (user.Flags.AsNullable() is {} userFlags && config.AccountFlagsBypass is {} bypassFlags)
        {
            if ((userFlags & bypassFlags) == userFlags)
            {
                threatScore = config.BaseJoinScore;
            }
        }

        _users.Add((user, joinTimestamp, threatScore, false));
    }

    /// <returns>Whether or not the current configuration would consider the current state to be a raid.</returns>
    public bool IsRaid(GuildAntiRaidConfigDTO config)
    {
        ClearState(config);
        var score = _users.Sum(u => u.ThreatScore);

        return score >= config.ThreatScoreThreshold;
    }

    /// <summary>
    /// Marks users as handled.
    /// </summary>
    /// <param name="users">The users to mark as handled.</param>
    public void MarkUsersHandled(IEnumerable<Snowflake> users)
    {
        foreach (var user in users)
        {
            var userState = _users.FirstOrDefault(u => u.User.ID == user);

            if (userState.User.ID == user)
            {
                userState.Handled = true;
            }
        }
    }

    /// <summary>
    /// Gets users that are considered suspicious.
    /// </summary>
    /// <param name="config">A config to determine which users should be considered suspicious.</param>
    /// <returns>Suspicious users, as determined by the config.</returns>
    public IEnumerable<IUser> GetSuspiciousUsers(GuildAntiRaidConfigDTO config)
    {
        ClearState(config);

        return _users.Where(u => u.ThreatScore > config.BaseJoinScore && !u.Handled).Select(u => u.User);
    }

    /// <summary>
    /// Clears the current state based on the cooldown period.
    /// </summary>
    /// <param name="config"></param>
    private void ClearState(GuildAntiRaidConfigDTO config)
    {
        var now = DateTimeOffset.UtcNow;
        var cutoff = now - config.AntiRaidCooldownPeriod;

        if (_users.LastOrDefault().JoinDate < cutoff)
        {
            _users.Clear();
            return;
        }

        _users.RemoveAll(u => u.JoinDate < cutoff);
    }
}
