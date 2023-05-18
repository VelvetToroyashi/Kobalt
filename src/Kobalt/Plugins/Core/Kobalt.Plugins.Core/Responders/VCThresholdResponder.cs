﻿using Kobalt.Plugins.Core.Services;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Extensions.Attributes;
using Remora.Discord.Gateway.Responders;

namespace Kobalt.Core.Responders;

[Responder]
public class VCThresholdResponder : IResponder<IGuildCreate>, IResponder<IVoiceStateUpdate>
{
    private readonly ChannelWatcherService _channels;
    private readonly ILogger<VCThresholdResponder> _logger;

    public VCThresholdResponder(ChannelWatcherService channels, ILogger<VCThresholdResponder> logger)
    {
        _channels = channels;
        _logger = logger;
    }

    public async Task<Result> RespondAsync(IGuildCreate gatewayEvent, CancellationToken ct = default)
    {
        if (gatewayEvent.Guild.IsT1)
        {
            return Result.FromSuccess();
        }

        var guild = gatewayEvent.Guild.AsT0;

        return await _channels.InitializeGuildAsync(guild.ID, guild.VoiceStates, ct);
    }

    public async Task<Result> RespondAsync(IVoiceStateUpdate gatewayEvent, CancellationToken ct = default)
        => await _channels.HandleStateUpdateAsync(gatewayEvent, ct);
}