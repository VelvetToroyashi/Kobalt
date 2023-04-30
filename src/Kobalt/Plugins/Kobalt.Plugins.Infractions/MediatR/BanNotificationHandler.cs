using Kobalt.Plugins.Infractions.Services;
using Kobalt.Shared.Mediator.Moderation;
using MediatR;

namespace Kobalt.Plugins.Infractions.MediatR;

public class BanNotificationHandler : INotificationHandler<BanUserNotification>
{
    private readonly InfractionAPIService _infractions;
    public BanNotificationHandler(InfractionAPIService infractions) => _infractions = infractions;

    public async Task Handle(BanUserNotification notification, CancellationToken cancellationToken)
    {
        await _infractions.BanUserAsync
        (
            notification.GuildID,
            notification.Target,
            notification.Moderator,
            notification.Reason,
            notification.Duration
        );
    }
}
