using System.Threading;
using System.Threading.Tasks;
using Kobalt.Bot.Services;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;

namespace Kobalt.Bot.Responders;

public class PhishingResponder : IResponder<IGuildMemberAdd>, IResponder<IGuildMemberUpdate>, IResponder<IMessageCreate>
{
    private readonly PhishingDetectionService _phishing;
    public PhishingResponder(PhishingDetectionService phishing) => _phishing = phishing;

    public Task<Result> RespondAsync(IGuildMemberAdd gatewayEvent, CancellationToken ct = default) => _phishing.HandleAsync(gatewayEvent.User.Value, gatewayEvent.GuildID, ct);
    public Task<Result> RespondAsync(IGuildMemberUpdate gatewayEvent, CancellationToken ct = default) => _phishing.HandleAsync(gatewayEvent.User, gatewayEvent.GuildID, ct);
    public Task<Result> RespondAsync(IMessageCreate gatewayEvent, CancellationToken ct = default) => _phishing.HandleAsync(gatewayEvent, gatewayEvent.GuildID, ct);
}
