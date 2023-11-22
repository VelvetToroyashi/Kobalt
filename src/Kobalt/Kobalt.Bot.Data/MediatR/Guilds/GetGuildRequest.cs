using Kobalt.Bot.Data.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Bot.Data.MediatR.Guilds;

public static partial class GetGuild
{
    public record Request(Snowflake GuildID) : IRequest<Result<KobaltGuild>>;
    
    internal class GetGuildHandler(IDbContextFactory<KobaltContext> context) : IRequestHandler<Request, Result<KobaltGuild>>
    {
        public async Task<Result<KobaltGuild>> Handle(Request request, CancellationToken cancellationToken)
        {
            await using var db = await context.CreateDbContextAsync(cancellationToken);

            var guild = await db.Guilds
                .Where(x => x.ID == request.GuildID)
                .Include(x => x.AutoModConfig)
                .Include(x => x.AntiRaidConfig)
                .Include(x => x.PhishingConfig)
                .Include(x => x.LogChannels)
                .FirstOrDefaultAsync(cancellationToken);

            if (guild is null)
            {
                return new NotFoundError($"No guild with the ID `{request.GuildID}` was found.");
            }

            return guild;
        }
    }
}
