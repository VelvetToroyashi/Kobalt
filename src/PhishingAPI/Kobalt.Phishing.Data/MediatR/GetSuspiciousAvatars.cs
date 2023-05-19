using Kobalt.Phishing.Data.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kobalt.Phishing.Data.MediatR;

public static class GetSuspiciousAvatars
{
    /// <summary>
    /// Requests for all suspicious avatars on a guild.
    /// </summary>
    /// <param name="GuildID">The ID of the guild.</param>
    public record Request(ulong GuildID) : IRequest<IEnumerable<SuspiciousAvatar>>;

    internal class Handler : IRequestHandler<Request, IEnumerable<SuspiciousAvatar>>
    {
        private readonly IDbContextFactory<PhishingContext> _context;

        public Handler(IDbContextFactory<PhishingContext> context) => _context = context;

        public async Task<IEnumerable<SuspiciousAvatar>> Handle(Request request, CancellationToken cancellationToken)
        {
            await using var context = await _context.CreateDbContextAsync(cancellationToken);

            return await context.SuspiciousAvatars
                .Where(x => x.GuildID == request.GuildID || x.GuildID == null)
                .ToListAsync(cancellationToken);
        }
    }
}
