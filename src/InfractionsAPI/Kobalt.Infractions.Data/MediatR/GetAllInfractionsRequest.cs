using Kobalt.Infractions.Infrastructure.Mediator.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kobalt.Infractions.Data.Mediator;

public record GetAllInfractionsRequest : IRequest<IEnumerable<InfractionDTO>>;

public class GetAllInfractionsHandler : IRequestHandler<GetAllInfractionsRequest, IEnumerable<InfractionDTO>>
{
    private readonly IDbContextFactory<InfractionContext> _context;

    public GetAllInfractionsHandler(IDbContextFactory<InfractionContext> context) => _context = context;

    public async Task<IEnumerable<InfractionDTO>> Handle(GetAllInfractionsRequest request, CancellationToken cancellationToken)
    {
        await using var context = await _context.CreateDbContextAsync(cancellationToken);

        return await context
                     .Infractions
                     .Where(inf => inf.IsProcessable)
                     .Select
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
                     )
                     .ToArrayAsync(cancellationToken);
    }
}
