using Kobalt.Phishing.Data.Entities;
using Kobalt.Phishing.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Remora.Results;

namespace Kobalt.Phishing.Data.MediatR;

public static class CreateSuspiciousUsername
{
    /// <summary>
    /// Represents a request to create a new username filter.
    /// </summary>
    /// <param name="GuildID">The ID of the guild to add this filter to.</param>
    /// <param name="UsernamePattern">The pattern of the username.</param>
    /// <param name="ParseType">The format for the username.</param>
    public record Request(ulong GuildID, string UsernamePattern, UsernameParseType ParseType) : IRequest<Result<SuspiciousUsername>>;

    internal class Handler : IRequestHandler<Request, Result<SuspiciousUsername>>
    {
        private readonly IDbContextFactory<PhishingContext> _context;

        public Handler(IDbContextFactory<PhishingContext> context) => _context = context;

        public async Task<Result<SuspiciousUsername>> Handle(Request request, CancellationToken cancellationToken)
        {
            await using var context = await _context.CreateDbContextAsync(cancellationToken);

            var preexisting = await context.SuspiciousUsernames
                .Where(x => x.GuildID == request.GuildID && x.UsernamePattern == request.UsernamePattern)
                .FirstOrDefaultAsync(cancellationToken);

            if (preexisting is not null)
            {
                return new InvalidOperationError("A username filter with this pattern already exists.");
            }

            var username = new SuspiciousUsername
            {
                GuildID = request.GuildID,
                UsernamePattern = request.UsernamePattern,
                ParseType = request.ParseType,
                CreatedBy = request.GuildID,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await context.SuspiciousUsernames.AddAsync(username, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            return Result<SuspiciousUsername>.FromSuccess(username);
        }
    }

}
