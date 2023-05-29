using Kobalt.Plugins.Core.Data.Mediator;
using MediatR;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;

namespace Kobalt.Core.Responders;

public class GuildResponder : IResponder<IGuildCreate>
{
    private readonly IMediator _mediator;
    public GuildResponder(IMediator mediator) => _mediator = mediator;

    public async Task<Result> RespondAsync(IGuildCreate gatewayEvent, CancellationToken ct = default)
    {
        if (gatewayEvent.Guild.Value is IGuildCreate.IAvailableGuild guild)
        {
            return await _mediator.Send(new AddGuild.Request(guild.ID), ct);
        }

        return Result.FromSuccess();
    }
}
