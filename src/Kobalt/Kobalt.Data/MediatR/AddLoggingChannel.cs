using Kobalt.Data.DTOs;
using Kobalt.Data.Entities;
using Kobalt.Shared.Types;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Data.Mediator;

/// <summary>
/// Represents a request to add a logging channel.
/// </summary>
public static class AddLoggingChannel
{
    /// <summary>
    /// Requests to add a logging channel.
    /// </summary>
    /// <param name="GuildID">The ID of the guild the channel belongs to.</param>
    /// <param name="ChannelID">The ID of the channel to log to.</param>
    /// <param name="Type">The type of logging the channel is used for.</param>
    public record Request(Snowflake GuildID, Snowflake ChannelID, LogChannelType Type) : IRequest<Result<LogChannelDTO>>;

    internal class Handler : IRequestHandler<Request, Result<LogChannelDTO>>
    {
        private readonly IDbContextFactory<KobaltContext> _context;

        public Handler(IDbContextFactory<KobaltContext> context) => _context = context;

        public async Task<Result<LogChannelDTO>> Handle(Request request, CancellationToken cancellationToken)
        {
            await using var context = await _context.CreateDbContextAsync(cancellationToken);

            var channel = new LogChannel
            {
                GuildID = request.GuildID,
                ChannelID = request.ChannelID,
                Type = request.Type
            };

            context.LogChannels.Add(channel);
            await context.SaveChangesAsync(cancellationToken);

            return new LogChannelDTO(channel.ChannelID, channel.WebhookID, channel.WebhookToken);
        }
    }
}
