using Kobalt.Phishing.Data.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kobalt.Phishing.Data.MediatR;

public static class GetSuspiciousUsernames
{
    /// <summary>
    /// Requests all suspicious usernames for a guild.
    /// </summary>
    /// <param name="GuildID">The ID of the guild to request usernames for.</param>
    public record Request(ulong GuildID) : IRequest<IEnumerable<SuspiciousUsername>>;

    internal class Handler : IRequestHandler<Request, IEnumerable<SuspiciousUsername>>
    {
        private readonly IDbContextFactory<PhishingContext> _context;

        public Handler(IDbContextFactory<PhishingContext> context) => _context = context;

        public async Task<IEnumerable<SuspiciousUsername>> Handle(Request request, CancellationToken cancellationToken)
        {
            await using var context = await _context.CreateDbContextAsync(cancellationToken);

            return await context.SuspiciousUsernames
                .Where(x => x.GuildID == request.GuildID || x.GuildID == null)
                .ToListAsync(cancellationToken);
        }
    }
}
