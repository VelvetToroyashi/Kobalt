using System.Collections.Concurrent;
using Kobalt.Plugins.Core.Data.Mediator;
using MediatR;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Plugins.Core.Services;

public class ChannelWatcherService
{
    // Mapping of Guild ID ➜ (User ID ➜ List of Voice States)
    private readonly ConcurrentDictionary<Snowflake, Dictionary<Snowflake, IPartialVoiceState>> _guildStates = new();

    private readonly IMediator _mediator;
    private readonly IDiscordRestChannelAPI _channels;
    private readonly ILogger<ChannelWatcherService> _logger;

    public ChannelWatcherService
    (
        IMediator mediator,
        IDiscordRestChannelAPI channels,
        ILogger<ChannelWatcherService> logger
    )
    {
        _mediator = mediator;
        _channels = channels;
        _logger = logger;
    }

    public async Task<Result> InitializeGuildAsync(Snowflake guildID, IReadOnlyList<IPartialVoiceState> states, CancellationToken ct = default)
    {
        var automodConfigResult = await _mediator.Send(new GetGuild.AutoModConfigRequest(guildID), ct);

        if (!automodConfigResult.IsSuccess)
        {
            return Result.FromSuccess();
        }

        var channelThreshold = automodConfigResult.Entity.PushToTalkThreshold;

        if (channelThreshold.GetValueOrDefault() is 0)
        {
            return Result.FromSuccess();
        }

        var channels = states.GroupBy(state => state.ChannelID);

        foreach (var channel in channels)
        {
            if (!channel.Key.IsDefined(out var channelID))
            {
                continue;
            }

            _guildStates[guildID] = channel.ToDictionary(state => state.UserID.Value, state => state);

            await HandleThresholdTransitionAsync(guildID, channelID.Value, channel.Count(), channelThreshold.Value, ct);
        }

        return Result.FromSuccess();
    }

    /// <summary>
    /// Handles an update to a user's voice state.
    /// </summary>
    /// <param name="newState"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<Result> HandleStateUpdateAsync(IVoiceState newState, CancellationToken ct)
    {
        if (!newState.GuildID.IsDefined(out var guildID))
        {
            return new InvalidOperationError("Voice state update did not contain a guild ID.");
        }

        var automodConfigResult = await _mediator.Send(new GetGuild.AutoModConfigRequest(guildID), ct);

        if (!automodConfigResult.IsSuccess)
        {
            return Result.FromSuccess();
        }

        var channelThreshold = automodConfigResult.Entity.PushToTalkThreshold;

        if (channelThreshold.GetValueOrDefault() is 0)
        {
            return Result.FromSuccess();
        }

        if (!_guildStates.TryGetValue(guildID, out var userStates))
        {
           userStates = _guildStates[guildID] = new Dictionary<Snowflake, IPartialVoiceState>();
        }

        _ = userStates.TryGetValue(newState.UserID, out var oldState);

        if (oldState is {} previousState && previousState.ChannelID.Value == newState.ChannelID)
        {
            return Result.FromSuccess();
        }

        if (!newState.ChannelID.HasValue)
        {
            userStates.Remove(newState.UserID);
        }
        else if (oldState?.ChannelID.Value != newState.ChannelID)
        {
            userStates[newState.UserID] = newState;

            var newChannelCount = userStates.Count(state => state.Value.ChannelID == newState.ChannelID);
            await HandleThresholdTransitionAsync(guildID, newState.ChannelID.Value, newChannelCount, channelThreshold.Value, ct);
        }

        if (oldState is not null)
        {
            var oldChannelCount = userStates.Count(state => state.Value.ChannelID == oldState.ChannelID);
            await HandleThresholdTransitionAsync(guildID, oldState.ChannelID.Value.Value, oldChannelCount, channelThreshold.Value, ct);
        }

        return Result.FromSuccess();
    }

    /// <summary>
    /// Handles transitioning a channel to or from Push-to-Talk mode.
    /// </summary>
    /// <param name="guildID">The ID of the guild to handle.</param>
    /// <param name="channelID">The ID of the channel to transition.</param>
    /// <param name="count">The current count of users in the channel.</param>
    /// <param name="channelThreshold">The threshold for push to talk to be toggled.</param>
    /// <param name="ct">A cancellation token.</param>
    private async Task HandleThresholdTransitionAsync(Snowflake guildID, Snowflake channelID, int count, int channelThreshold, CancellationToken ct)
    {

        var channelResult = await _channels.GetChannelAsync(channelID, ct);

        if (!channelResult.IsSuccess)
        {
            return;
        }

        var channel = channelResult.Entity;

        var permissionResult = GetNewPermissions(guildID, count, channel, channelThreshold);

        if (!permissionResult.IsDefined(out var newPermissions))
        {
            return;
        }

        var updateResult = await _channels.EditChannelPermissionsAsync
        (
            channelID,
            guildID,
            (DiscordPermissionSet)newPermissions.Allow,
            (DiscordPermissionSet)newPermissions.Deny,
            reason: "AutoMod: Voice Channel Threshold Updated.",
            ct: ct
        );

        if (!updateResult.IsSuccess)
        {
            _logger.LogWarning("Failed to update channel permissions for {ChannelID} in {GuildID}.", channelID, guildID);
        }
    }

    /// <summary>
    /// Gets the new permissions for a given channel, based on the current threshold and the number of users in the channel.
    /// </summary>
    /// <param name="guildID">The ID of the guild the channel resides in.</param>
    /// <param name="count">The amount of users currently present in the channel.</param>
    /// <param name="channel">The ID of the channel.</param>
    /// <param name="channelThreshold">The threshold of users for the channel to be switched to/from PTT.</param>
    /// <returns>A successful result if the permission should be transitioned, otherwise a <see cref="InvalidOperationError"/>. </returns>
    private static Result<IPermissionOverwrite> GetNewPermissions(Snowflake guildID, int count, IChannel channel, int? channelThreshold)
    {
        var everyoneOverwrite = channel.PermissionOverwrites.AsNullable()?.FirstOrDefault(overwrite => overwrite.ID == guildID);
        everyoneOverwrite ??= new PermissionOverwrite(guildID, PermissionOverwriteType.Role, DiscordPermissionSet.Empty, DiscordPermissionSet.Empty);

        var newPermissions = new PermissionOverwrite(guildID, PermissionOverwriteType.Role, everyoneOverwrite.Allow, everyoneOverwrite.Deny);

        if (count >= channelThreshold)
        {
            var denyValue = everyoneOverwrite.Deny.Value | 1 << (int)DiscordVoicePermission.UseVoiceActivity;
            var denyPermissions = new DiscordPermissionSet(denyValue);

            newPermissions = newPermissions with { Deny = denyPermissions };
        }
        else
        {
            var denyValue = everyoneOverwrite.Deny.Value & ~(1 << (int)DiscordVoicePermission.UseVoiceActivity);
            var denyPermissions = new DiscordPermissionSet(denyValue);

            newPermissions = newPermissions with { Deny = denyPermissions };
        }

        var alreadyHasPermissions = everyoneOverwrite.Deny.Value == newPermissions.Deny.Value;

        if (alreadyHasPermissions)
        {
            return new InvalidOperationError("The channel already has the applied permissions.");
        }

        return newPermissions;
    }
}
