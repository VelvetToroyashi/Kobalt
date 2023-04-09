using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;
using Kobalt.Infractions.Infrastructure.Interfaces;
using Kobalt.Infractions.Infrastructure.Mediator;
using Kobalt.Infractions.Infrastructure.Mediator.DTOs;
using Kobalt.Infractions.Shared;
using Kobalt.Shared.Services;
using Mediator;
using Microsoft.Extensions.Options;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Infractions.API.Services;

public class InfractionService : BackgroundService, IInfractionService
{
    private readonly IMediator _mediator;
    private readonly JsonSerializerOptions _serializer;
    private readonly WebsocketManagerService _socketManager;
    
    private readonly PeriodicTimer _dispatcherTimer;
    private readonly SemaphoreSlim _dispatcherLock = new(1, 1);
    private readonly Channel<InfractionDTO> _dispatcherChannel;
    private readonly ConcurrentDictionary<int, InfractionDTO> _infractions = new();
    
    private CancellationToken _cancellationToken;

    private Task _queueTask = Task.CompletedTask,
                 _dispatcherTask = Task.CompletedTask;

    /// <summary>
    /// Creates a new <see cref="InfractionService"/>.
    /// </summary>
    /// <param name="mediator">A mediator.</param>
    /// <param name="socketManager">A websocket manager.</param>
    public InfractionService(IMediator mediator, IOptions<JsonSerializerOptions> jsonOptions, WebsocketManagerService socketManager)
    {
        _mediator = mediator;
        _serializer = jsonOptions.Value;
        _socketManager = socketManager;
        
        _dispatcherChannel = Channel.CreateUnbounded<InfractionDTO>();
        _dispatcherTimer = new(TimeSpan.FromSeconds(1));
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
        DateTimeOffset? expiresAt = null,
        int? referencedID = null
    )
    {
        if (expiresAt is not null && expiresAt < DateTimeOffset.UtcNow)
            return new InvalidOperationError("The expiration date must be in the future.");
        
        if (type is not (InfractionType.Mute or InfractionType.Ban) && expiresAt is not null)
            return new InvalidOperationError("Only mutes and bans can have an expiration date.");

        var infraction = await _mediator.Send(new CreateInfractionRequest(reason, userID, guildID, moderatorID, type, DateTimeOffset.UtcNow, expiresAt));

        if (expiresAt is not null)
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
    async Task<Result<InfractionDTO>> IInfractionService.UpdateInfractionAsync(int id, Optional<string> reason, Optional<bool> isHidden, Optional<DateTimeOffset?> expiresAt)
    {
        if (!reason.HasValue && !isHidden.HasValue && !expiresAt.HasValue)
            return new InvalidOperationError("You must provide at least one value to update.");
        
        if (expiresAt.IsDefined(out var expiration) && expiration < DateTimeOffset.UtcNow)
            return new InvalidOperationError("The expiration date must be in the future.");
            
        var updateResult = await _mediator.Send(new UpdateInfractionRequest(id, isHidden, reason, expiresAt));

        if (!updateResult.IsSuccess)
            return updateResult;
        
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

    private async Task UnloadQueueAsync()
    {
        while (await _dispatcherTimer.WaitForNextTickAsync(_cancellationToken))
        {
            await _dispatcherLock.WaitAsync(_cancellationToken);

            foreach (var infraction in _infractions.Values)
            {
                if (infraction.ExpiresAt > DateTimeOffset.UtcNow)
                {
                    _infractions.Remove(infraction.Id, out _);
                    await _dispatcherChannel.Writer.WriteAsync(infraction, _cancellationToken);
                    continue;
                }
            }
        }
    }

    // Should this be it's own service? 
    private async Task DispatchAsync()
    {
        while (await _dispatcherChannel.Reader.WaitToReadAsync(_cancellationToken))
        {
            var dto = await _dispatcherChannel.Reader.ReadAsync();
            
            var json = JsonSerializer.Serialize(dto);
        }
    }

}
