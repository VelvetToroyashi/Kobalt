using System.Threading.Channels;
using Kobalt.Infractions.Infrastructure.Interfaces;
using Kobalt.Infractions.Infrastructure.Mediator.DTOs;
using Mediator;
using Remora.Rest;

namespace Kobalt.Infractions.API.Services;

public class InfractionService : BackgroundService, IInfractionService
{
    private readonly IMediator _mediator;
    private readonly IRestHttpClient _httpClient;
    private readonly List<InfractionDTO> _infractions = new();
    private readonly Channel<InfractionDTO> _dispatcherChannel;
    private readonly PeriodicTimer _dispatcherTimer, _queueTimer;

    private Task _queueTask,
                 _dispatcherTask;

    public InfractionService(IMediator mediator, IRestHttpClient httpClient)
    {
        _mediator = mediator;
        _httpClient = httpClient;
        _dispatcherChannel = Channel.CreateUnbounded<InfractionDTO>();

        // The discrepancy between the queue timer and the dispatcher timer is intentional.
        // The ~500ms difference is to prevent the race condition of dispatching an infraction
        // when it's being updated, which could remove it from the queue. In that case the client
        // will be notified about the infraction twice, which is an issue, as the client may attempt
        // to act upon that notification (e.g. unmuting the infraction target).
        // Perhaps there's a better way to handle the race condition?
        _dispatcherTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        _queueTimer = new PeriodicTimer(TimeSpan.FromSeconds(1.5));
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
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
    
    private async Task UnloadQueueAsync(){}
    
    private async Task DispatchAsync(){}
    
}
