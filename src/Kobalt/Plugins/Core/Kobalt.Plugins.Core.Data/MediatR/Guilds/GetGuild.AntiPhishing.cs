using Kobalt.Plugins.Core.Data.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Plugins.Core.Data.Mediator;

public static partial class GetGuild
{
    public record PhishingConfigRequest(Snowflake GuildID) : IRequest<Result<GuildPhishingConfig>>;

    internal class Handler : IRequestHandler<PhishingConfigRequest, Result<GuildPhishingConfig>>
    {
        private readonly IDbContextFactory<KobaltContext> _context;
        public Handler(IDbContextFactory<KobaltContext> context) => _context = context;

        public async Task<Result<GuildPhishingConfig>> Handle(PhishingConfigRequest request, CancellationToken cancellationToken = default)
        {
            await using var context = await _context.CreateDbContextAsync(cancellationToken);

            var config = await context.Guilds
                                      .Where(g => g.ID == request.GuildID)
                                      .Select(g => g.PhishingConfig)
                                      .AsNoTracking()
                                      .FirstOrDefaultAsync(cancellationToken);


            if (config is null)
            {
                return new NotFoundError();
            }

            return config;
        }
    }
}
