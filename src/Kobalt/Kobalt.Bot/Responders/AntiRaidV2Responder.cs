using Kobalt.Bot.Services;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;

namespace Kobalt.Bot.Responders;

/// <summary>
/// Pipes incoming joins to the anti-raid service.
/// </summary>
public class AntiRaidV2Responder : IResponder<IGuildMemberAdd>
{
    private readonly AntiRaidV2Service _service;
    public AntiRaidV2Responder(AntiRaidV2Service service) => _service = service;

    public Task<Result> RespondAsync(IGuildMemberAdd gatewayEvent, CancellationToken ct = default) => _service.HandleAsync(gatewayEvent);
}
