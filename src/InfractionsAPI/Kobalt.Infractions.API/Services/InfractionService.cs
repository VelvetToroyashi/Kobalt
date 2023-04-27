using System.Collections.Concurrent;
using System.Threading.Channels;
using Kobalt.Infractions.Data.Mediator;
using Kobalt.Infractions.Infrastructure.Interfaces;
using Kobalt.Infractions.Infrastructure.Mediator;
using Kobalt.Infractions.Infrastructure.Mediator.DTOs;
using Kobalt.Infractions.Infrastructure.Mediator.Mediator;
using Kobalt.Infractions.Shared;
using MassTransit;
using Mediator;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Infractions.API.Services;

public class InfractionService : BackgroundService, IInfractionService
{
    private readonly IBus _bus;
    private readonly IMediator _mediator;
    private readonly PeriodicTimer _dispatcherTimer;
    private readonly Channel<InfractionDTO> _dispatcherChannel;
    private readonly ConcurrentDictionary<int, InfractionDTO> _infractions = new();

    private CancellationToken _cancellationToken;

    private Task _queueTask = Task.CompletedTask,
                 _dispatcherTask = Task.CompletedTask;

    /// <summary>
    /// Creates a new <see cref="InfractionService"/>.
    /// </summary>
    /// <param name="bus">RabbitMQ message bus</param>
    /// <param name="mediator">A mediator.</param>
    public InfractionService(IBus bus, IMediator mediator)
    {
        _bus = bus;
        _mediator = mediator;
        _dispatcherChannel = Channel.CreateUnbounded<InfractionDTO>();
        _dispatcherTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(400));
    }

    /// <inheritdoc/>
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken).Token;

        _queueTask = Task.Run(UnloadQueueAsync, stoppingToken);
        _dispatcherTask = Task.Run(DispatchAsync, stoppingToken);

        return Task.WhenAll(_queueTask, _dispatcherTask);
    }

    /// <inheritdoc/>
    async Task<Result<InfractionDTO>> IInfractionService.CreateInfractionAsync
    (
        ulong guildID,
        ulong userID,
        ulong moderatorID,
        InfractionType type,
        string reason,
        DateTimeOffset? expiresAt,
        int? referencedID
    )
    {
        if (expiresAt is not null && expiresAt < DateTimeOffset.UtcNow)
        {
            return new InvalidOperationError("The expiration date must be in the future.");
        }

        if (type is not (InfractionType.Mute or InfractionType.Ban) && expiresAt is not null)
        {
            return new InvalidOperationError("Only mutes and bans can have an expiration date.");
        }

        var infraction = await _mediator.Send(new CreateInfractionRequest(reason, userID, guildID, moderatorID, type, DateTimeOffset.UtcNow, expiresAt));

        if (expiresAt is not null)
        {
            _infractions.AddOrUpdate(infraction.Id, infraction, (_, _) => infraction);
        }
        else if (type is InfractionType.Ban or InfractionType.Mute)
        {
            _infractions.TryRemove(infraction.Id, out _);
        }

        return infraction;
    }

    /// <inheritdoc/>
    async Task<Result<InfractionDTO>> IInfractionService.UpdateInfractionAsync(int id, ulong guildID, Optional<string?> reason, Optional<bool> isHidden, Optional<DateTimeOffset?> expiresAt)
    {
        if (!reason.HasValue && !isHidden.HasValue && !expiresAt.HasValue)
        {
            return new InvalidOperationError("You must provide at least one value to update.");
        }

        if (expiresAt.IsDefined(out var expiration) && expiration < DateTimeOffset.UtcNow)
        {
            return new InvalidOperationError("The expiration date must be in the future.");
        }

        var updateResult = await _mediator.Send(new UpdateInfractionRequest(id, guildID, isHidden, reason, expiresAt));

        if (!updateResult.IsSuccess)
        {
            return updateResult;
        }

        var infraction = updateResult.Entity;

        if (expiresAt.IsDefined())
        {
            _infractions.AddOrUpdate(infraction.Id, infraction, (_, _) => infraction);
        }
        else
        {
            _infractions.TryRemove(infraction.Id, out _);
        }

        return infraction;
    }

    /// <inheritdoc/>
    public async Task<Optional<InfractionRuleMatch>> EvaluateInfractionsAsync(ulong guildID, ulong userID)
    {
        var rules = await _mediator.Send(new GetGuildInfractionRulesRequest(guildID));

        if (!rules.Any())
        {
            return default;
        }

        var infractions = await _mediator.Send(new GetInfractionsForUserRequest(guildID, userID));

        if (!infractions.Any())
        {
            return default;
        }

        // Get all infractions that have not been pardoned, else we'd erroneously
        // esclate infractions retroactively.
        var nonPardonedInfractions = infractions
                                    .Where(inf => inf.Type is >= InfractionType.Kick and <= InfractionType.Warning)
                                    .Where(inf => infractions.All(i => i.ReferencedId != inf.Id))
                                    .ToArray();

        if (!nonPardonedInfractions.Any())
        {
            return default;
        }

        // Group so we can avoid potentially making multiple queries for the same type.
        foreach (var ruleGroup in rules.GroupBy(g => g.MatchType))
        {
            var preliminaryFilter = nonPardonedInfractions.Where(inf => inf.Type == ruleGroup.Key).ToArray();

            foreach (var rule in ruleGroup)
            {
                var matchPeriod = rule.EffectiveTimespan ?? TimeSpan.MaxValue;
                var meetsThreshold = preliminaryFilter.Length >= rule.MatchValue;
                var meetsTimeThreshold = preliminaryFilter.Count(inf => inf.CreatedAt > DateTimeOffset.UtcNow.Add(-matchPeriod)) >= rule.MatchValue;

                if (meetsThreshold && meetsTimeThreshold)
                {
                    return new InfractionRuleMatch(rule.ActionType, rule.ActionDuration);
                }
            }
        }

        return default;
    }

    private async Task UnloadQueueAsync()
    {
        var infractions = await _mediator.Send(new GetAllInfractionsRequest(), _cancellationToken);

        foreach (var inf in infractions)
            _infractions.TryAdd(inf.Id, inf);

        while (await _dispatcherTimer.WaitForNextTickAsync(_cancellationToken))
        {
            foreach (var infraction in _infractions.Values)
            {
                if (infraction.ExpiresAt > DateTimeOffset.UtcNow)
                {
                    continue;
                }

                _infractions.Remove(infraction.Id, out _);
                await _dispatcherChannel.Writer.WriteAsync(infraction, _cancellationToken);
            }
        }
    }

    // Should this be it's own service?
    private async Task DispatchAsync()
    {
        while (await _dispatcherChannel.Reader.WaitToReadAsync(_cancellationToken))
        {
            var dto = await _dispatcherChannel.Reader.ReadAsync(CancellationToken.None);

            await _bus.Publish(dto, _cancellationToken);

            if (dto.Type is InfractionType.Ban or InfractionType.Mute)
            {
                await _mediator.Send(new UpdateInfractionRequest(dto.Id, dto.GuildID, default, default, default, false));
            }
        }
    }
}
