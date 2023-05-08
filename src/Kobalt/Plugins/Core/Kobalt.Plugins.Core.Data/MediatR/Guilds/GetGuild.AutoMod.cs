using Kobalt.Plugins.Core.Data.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Plugins.Core.Data.Mediator;

public static partial class GetGuild
{
    /// <summary>
    /// Requests the auto-mod configuration for a guild.
    /// </summary>
    /// <param name="GuildID">The ID of the guild to get the config for.</param>
    public record AutoModConfigRequest(Snowflake GuildID) : IRequest<Result<GuildAutoModConfig>>;

    internal class AutoModConfigHandler : IRequestHandler<AutoModConfigRequest, Result<GuildAutoModConfig>>
    {
        private readonly IDbContextFactory<KobaltContext> _context;

        public AutoModConfigHandler(IDbContextFactory<KobaltContext> context) => _context = context;

        public async Task<Result<GuildAutoModConfig>> Handle(AutoModConfigRequest request, CancellationToken ct)
        {
            await using var context = await _context.CreateDbContextAsync(ct);

            var config = await context.Guilds
                                      .Where(g => g.ID == request.GuildID)
                                      .Select(g => g.AutoModConfig)
                                      .AsNoTracking()
                                      .FirstOrDefaultAsync(ct);

            if (config is null)
            {
                return new NotFoundError();
            }

            return config;
        }
    }
}
