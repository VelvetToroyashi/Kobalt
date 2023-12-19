using Kobalt.Bot.Data.DTOs;
using Kobalt.Bot.Data.Entities;
using Kobalt.Shared.Types;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Remora.Rest.Core;

namespace Kobalt.Bot.Data.MediatR;

/// <summary>
/// Represents a request to get logging channels.
/// </summary>
public static class GetLoggingChannels
{
    private const string GetLogChannelSql = 
    """
    SELECT channel_id, webhook_id, webhook_token, type
    FROM kobalt_core.log_channels
    WHERE guild_id = @guild_id AND ((type::bigint)::bit(64) & (@type::bigint)::bit(64) = (@type::bigint)::bit(64))
    """;
    
    /// <summary>
    /// Requests logging channels of a given type for a given guild.
    /// </summary>
    /// <param name="GuildID">The ID of the guild to fetch channels for.</param>
    /// <param name="Type">The type of channel to fetch logs for.</param>
    public record Request(Snowflake GuildID, LogChannelType Type) : IRequest<IReadOnlyList<LogChannelDTO>>;

    internal class Handler : IRequestHandler<Request, IReadOnlyList<LogChannelDTO>>
    {
        private readonly IDbContextFactory<KobaltContext> _context;

        public Handler(IDbContextFactory<KobaltContext> context) => _context = context;

        public async Task<IReadOnlyList<LogChannelDTO>> Handle(Request request, CancellationToken cancellationToken)
        {
            await using var context = await _context.CreateDbContextAsync(cancellationToken);

            var channels = await context
                                 .LogChannels
                                 .FromSqlRaw(GetLogChannelSql, request.GuildID.Value, request.Type)
                                 .Select(l => new LogChannelDTO(l.ChannelID, l.WebhookID, l.WebhookToken, l.Type))
                                 .ToListAsync(CancellationToken.None);

            return channels;
        }
    }

}
