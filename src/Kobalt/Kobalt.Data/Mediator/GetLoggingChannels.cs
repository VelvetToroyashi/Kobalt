using Kobalt.Data.DTOs;
using Kobalt.Shared.Types;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Remora.Rest.Core;

namespace Kobalt.Data.Mediator;

/// <summary>
/// Represents a request to get logging channels.
/// </summary>
public static class GetLoggingChannels
{
    /// <summary>
    /// Requests logging channels of a given type for a given guild.
    /// </summary>
    /// <param name="GuildID">The ID of the guild to fetch channels for.</param>
    /// <param name="Type">The type of channel to fetch logs for.</param>
    public record Request(Snowflake GuildID, LogChannelType Type) : IRequest<IReadOnlyList<LogChannelDTO>>;
    
    internal class Handler : IRequestHandler<Request, IReadOnlyList<LogChannelDTO>>
    {
        private readonly IDbContextFactory<KobaltContext> _context;

        public Handler(IDbContextFactory<KobaltContext> context)
        {
            _context = context;
        }

        public async ValueTask<IReadOnlyList<LogChannelDTO>> Handle(Request request, CancellationToken cancellationToken)
        {
            await using var context = await _context.CreateDbContextAsync(cancellationToken);
            
            var channels = await context.LogChannels
                .Where(l => l.GuildID == request.GuildID && l.Type == request.Type)
                .Select(l => new LogChannelDTO(l.ChannelID, l.WebhookID, l.WebhookToken))
                .ToListAsync(cancellationToken);

            return channels;
        }
    }
    
}
