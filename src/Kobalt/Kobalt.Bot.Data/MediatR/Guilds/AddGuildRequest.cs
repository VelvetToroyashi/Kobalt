using Kobalt.Bot.Data.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Bot.Data.MediatR.Guilds;

public static class AddGuild
{
    public record Request(Snowflake GuildID) : IRequest<Result>;

    internal class Handler : IRequestHandler<Request, Result>
    {
        private readonly IDbContextFactory<KobaltContext> _context;

        public Handler(IDbContextFactory<KobaltContext> context) => _context = context;

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

            db.Guilds.Add
            (
                new KobaltGuild
                {
                    ID = request.GuildID,
                    AutoModConfig  = new GuildAutoModConfig { GuildID = request.GuildID },
                    AntiRaidConfig = GuildAntiRaidConfig.Default(request.GuildID),
                    PhishingConfig = new GuildPhishingConfig { GuildID = request.GuildID },
                }
            );

            await db.SaveChangesAsync(cancellationToken);

            return Result.FromSuccess();
        }
    }
}
