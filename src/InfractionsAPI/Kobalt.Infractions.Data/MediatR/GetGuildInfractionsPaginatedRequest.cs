using Kobalt.Infractions.Shared.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kobalt.Infractions.Data.MediatR;

public static class GetGuildInfractionsPaginated
{
    public record Request(ulong GuildID, int Page = 1, int PageSize = 10) : IRequest<IEnumerable<InfractionDTO>>;
    
    internal class Handler(IDbContextFactory<InfractionContext> context) : IRequestHandler<Request, IEnumerable<InfractionDTO>>
    {

        public async Task<IEnumerable<InfractionDTO>> Handle(Request request, CancellationToken cancellationToken)
        {
            await using var db = await context.CreateDbContextAsync(cancellationToken);

            var infractions = await db.Infractions
                .Where(x => x.GuildID == request.GuildID)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToArrayAsync(cancellationToken);

            return infractions.Select
            (
                x => new InfractionDTO
                (
                    x.Id,
                    x.ReferencedId,
                    x.IsHidden,
                    x.Reason,
                    x.UserID,
                    x.GuildID,
                    x.ModeratorID,
                    x.Type,
                    x.CreatedAt,
                    x.ExpiresAt
                )
            );
        }
    }
}

