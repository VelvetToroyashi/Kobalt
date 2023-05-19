using Kobalt.Phishing.Data.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Remora.Results;

namespace Kobalt.Phishing.Data.MediatR;

public static class CreateSuspiciousAvatar
{
    /// <summary>
    /// Represents a request to create a new avatar filter.
    /// </summary>
    /// <param name="GuildID">The ID of the guild to add the avatar to.</param>
    /// <param name="Md5Hash">The optional MD5 hash of the image.</param>
    /// <param name="AddedBy">Who this image was added by.</param>
    /// <param name="Source">The source of this image, e.g. twitter, discord, etc.</param>
    /// <param name="Category">The category of this image, e.g. `discord_modmail`.</param>
    /// <param name="Phash">The phash of this image.</param>
    public record Request
    (
        ulong GuildID,
        string? Md5Hash,
        ulong AddedBy,
        string Source,
        string Category,
        byte[] Phash
    ) : IRequest<Result<SuspiciousAvatar>>;

    internal class Handler : IRequestHandler<Request, Result<SuspiciousAvatar>>
    {
        private readonly IDbContextFactory<PhishingContext> _context;

        public Handler(IDbContextFactory<PhishingContext> context) => _context = context;

        public async Task<Result<SuspiciousAvatar>> Handle(Request request, CancellationToken cancellationToken)
        {
            await using var context = await _context.CreateDbContextAsync(cancellationToken);

            var preexisting = await context
                                   .SuspiciousAvatars
                                   .FirstOrDefaultAsync
                                    (
                                        x =>
                                        (x.GuildID == request.GuildID || x.GuildID == null) &&
                                        (x.Md5Hash == request.Md5Hash || x.Category == request.Category),
                                        cancellationToken
                                    );

            if (preexisting is not null)
            {
                return new InvalidOperationError("Avatar already exists.");
            }

            var avatar = new SuspiciousAvatar
            {
                GuildID = request.GuildID,
                Md5Hash = request.Md5Hash,
                AddedBy = request.AddedBy,
                Source = request.Source,
                Category = request.Category,
                Phash = request.Phash,
                AddedAt = DateTimeOffset.UtcNow
            };

            context.SuspiciousAvatars.Add(avatar);
            await context.SaveChangesAsync(cancellationToken);

            return avatar;
        }
    }

}
