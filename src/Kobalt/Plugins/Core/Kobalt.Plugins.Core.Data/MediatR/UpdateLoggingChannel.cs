using Kobalt.Plugins.Core.Data.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Plugins.Core.Data.Mediator;

public static class UpdateLoggingChannel
{
    public record Request(Snowflake ChannelID, Snowflake? WebhookID, string? WebhookToken) : IRequest<Result<LogChannelDTO>>;

    internal class Handler : IRequestHandler<Request,Result<LogChannelDTO>>
    {
        private readonly IDbContextFactory<KobaltContext> _context;

        public Handler(IDbContextFactory<KobaltContext> context) => _context = context;

        public async Task<Result<LogChannelDTO>> Handle(Request request, CancellationToken cancellationToken)
        {
            await using var context = await _context.CreateDbContextAsync(cancellationToken);

            var channel = await context.LogChannels.FirstOrDefaultAsync(l => l.ChannelID == request.ChannelID, cancellationToken);
            if (channel is null)
            {
                return new NotFoundError();
            }

            channel.WebhookID = request.WebhookID;
            channel.WebhookToken = request.WebhookToken;

            await context.SaveChangesAsync(cancellationToken);

            return new LogChannelDTO(channel.ChannelID, channel.WebhookID, channel.WebhookToken);
        }
    }

}
