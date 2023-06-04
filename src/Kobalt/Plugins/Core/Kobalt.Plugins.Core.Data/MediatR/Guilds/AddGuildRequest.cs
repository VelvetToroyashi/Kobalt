using Kobalt.Plugins.Core.Data.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Plugins.Core.Data.Mediator;

public static class AddGuild
{
    public record Request(Snowflake GuildID) : IRequest<Result>;

    internal class Handler : IRequestHandler<Request, Result>
    {
        private readonly IDbContextFactory<KobaltContext> _context;

        public async Task<Result> Handle(Request request, CancellationToken cancellationToken)
        {
            await using var db = await _context.CreateDbContextAsync(cancellationToken);

            var guild = await db.Guilds
                .Where(x => x.ID == request.GuildID)
                .FirstOrDefaultAsync(cancellationToken);

            if (guild is not null)
            {
                return Result.FromSuccess();
            }

            db.Guilds.Add(new Guild() { ID = request.GuildID });

            await db.SaveChangesAsync(cancellationToken);

            return Result.FromSuccess();
        }
    }
}