using Kobalt.Bot.Data.Entities.Phishing;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kobalt.Bot.Data.MediatR.Phishing;

public static class GetSuspiciousUsernames
{
    /// <summary>
    /// Requests all suspicious usernames for a guild.
    /// </summary>
    /// <param name="GuildID">The ID of the guild to request usernames for.</param>
    public record Request(ulong GuildID) : IRequest<IEnumerable<SuspiciousUsername>>;

    internal class Handler : IRequestHandler<Request, IEnumerable<SuspiciousUsername>>
    {
        private readonly IDbContextFactory<KobaltContext> _context;

        public Handler(IDbContextFactory<KobaltContext> context) => _context = context;

        public async Task<IEnumerable<SuspiciousUsername>> Handle(Request request, CancellationToken cancellationToken)
        {
            await using var context = await _context.CreateDbContextAsync(cancellationToken);

            return await context.SuspiciousUsernames
                .Where(x => x.GuildID == request.GuildID || x.GuildID == null)
                .ToListAsync(cancellationToken);
        }
    }
}
