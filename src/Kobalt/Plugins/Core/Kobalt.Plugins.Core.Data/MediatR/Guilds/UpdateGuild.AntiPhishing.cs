using Kobalt.Infractions.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Plugins.Core.Data.Mediator;

public static partial class UpdateGuild
{
    public static class AntiPhishing
    {
        /// <summary>
        /// Represents a request to update the phishing config of a guild.
        /// </summary>
        /// <param name="GuildID">The ID of the guild.</param>
        /// <param name="ScanUsers">Whether to scan users.</param>
        /// <param name="ScanLinks">Whether to scan links.</param>
        /// <param name="Action">The action to take when a phishing link is detected.</param>
        public record Request(Snowflake GuildID, Optional<bool> ScanUsers, Optional<bool> ScanLinks, Optional<InfractionType> Action) : IRequest<Result>;

        internal class Handler : IRequestHandler<Request, Result>
        {
            private readonly IDbContextFactory<KobaltContext> _context;

            public Handler(IDbContextFactory<KobaltContext> context) => _context = context;

            public async Task<Result> Handle(Request request, CancellationToken cancellationToken)
            {
                await using var db = await _context.CreateDbContextAsync(cancellationToken);

                var config = await db.Guilds
                    .Where(x => x.ID == request.GuildID)
                    .Select(x => x.PhishingConfig)
                    .FirstOrDefaultAsync(cancellationToken);

                if (config is null)
                {
                    return new NotFoundError();
                }

                if (request.ScanUsers.IsDefined(out var scanUsers))
                {
                    config.ScanUsers = scanUsers;
                }

                if (request.ScanLinks.IsDefined(out var scanLinks))
                {
                    config.ScanLinks = scanLinks;
                }

                if (request.Action.IsDefined(out var action))
                {
                    config.DetectionAction = action;
                }

                await db.SaveChangesAsync(cancellationToken);

                return Result.FromSuccess();
            }
        }
    }
}
