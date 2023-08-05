using Kobalt.Bot.Services;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;

namespace Kobalt.Bot.Responders;

public class PhishingResponder(PhishingDetectionService phishing) : IResponder<IGuildMemberAdd>, IResponder<IGuildMemberUpdate>, IResponder<IMessageCreate>
{
    public Task<Result> RespondAsync(IGuildMemberAdd gatewayEvent, CancellationToken ct = default) => phishing.HandleAsync(gatewayEvent.User.Value, gatewayEvent.GuildID, ct);
    public Task<Result> RespondAsync(IGuildMemberUpdate gatewayEvent, CancellationToken ct = default) => phishing.HandleAsync(gatewayEvent.User, gatewayEvent.GuildID, ct);
    public Task<Result> RespondAsync(IMessageCreate gatewayEvent, CancellationToken ct = default) => phishing.HandleAsync(gatewayEvent, gatewayEvent.GuildID, ct);
}
