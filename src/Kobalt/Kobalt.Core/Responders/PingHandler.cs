using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Gateway.Responders;

namespace Kobalt.Core.Responders;

public class PingHandler : IResponder<IInteractionCreate>
{
    private readonly IDiscordRestInteractionAPI _api;
    public PingHandler(IDiscordRestInteractionAPI api) => _api = api;

    public Task<Result> RespondAsync(IInteractionCreate gatewayEvent, CancellationToken ct = default)
    {
        if (gatewayEvent.Type is not InteractionType.Ping)
        {
            return Task.FromResult(Result.FromSuccess());
        }

        return _api.CreateInteractionResponseAsync(gatewayEvent.ID, gatewayEvent.Token, new InteractionResponse(InteractionCallbackType.Pong), ct: ct);
    }
}
