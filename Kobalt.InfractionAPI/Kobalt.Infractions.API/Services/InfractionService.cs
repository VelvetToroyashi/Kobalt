using System.Threading.Channels;
using Kobalt.Infractions.Infrastructure.Interfaces;
using Kobalt.Infractions.Infrastructure.Mediator.DTOs;
using Mediator;
using Remora.Rest;
using Remora.Rest.Extensions;

namespace Kobalt.Infractions.API.Services;

public class InfractionService : BackgroundService, IInfractionService
{
    private readonly IMediator _mediator;
    private readonly IRestHttpClient _httpClient;
    private readonly List<InfractionDTO> _infractions = new();
    private readonly Channel<InfractionDTO> _dispatcherChannel;
    private readonly PeriodicTimer _dispatcherTimer;
    private readonly SemaphoreSlim _dispatcherLock = new(1, 1);
    
    private CancellationToken _cancellationToken;

    private Task _queueTask,
                 _dispatcherTask;

    public InfractionService(IMediator mediator, IRestHttpClient httpClient)
    {
        _mediator = mediator;
        _httpClient = httpClient;
        _dispatcherChannel = Channel.CreateUnbounded<InfractionDTO>();
        
        _dispatcherTimer = new(TimeSpan.FromSeconds(1));
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken).Token;
        
        _queueTask = Task.Run(UnloadQueueAsync, stoppingToken);
        _dispatcherTask = Task.Run(DispatchAsync, stoppingToken);

        return Task.WhenAll(_queueTask, _dispatcherTask);
    }

    void IInfractionService.HandleInfractionUpdate(InfractionDTO infraction)
    {
        var existing = _infractions.FirstOrDefault(x => x.Id == infraction.Id);
        
        if (existing is not null)
        {
            _infractions.Remove(existing);
        }
        
        if (infraction.ExpiresAt is not null)
        {
            _infractions.Add(infraction);
        }
    }

    private async Task UnloadQueueAsync()
    {
        while (await _dispatcherTimer.WaitForNextTickAsync(_cancellationToken))
        {
            await _dispatcherLock.WaitAsync(_cancellationToken);

            foreach (var infraction in _infractions)
            {
                if (infraction.ExpiresAt > DateTimeOffset.UtcNow)
                {
                    _infractions.Remove(infraction);
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
            
            //await _mediator.Send(new InfractionExpiredNotification(dto));
            
            await _httpClient.PostAsync("/api/bot/infractions/expired", b => b.WithJson(json => json.Write("", dto)));
        }
    }
    
}
