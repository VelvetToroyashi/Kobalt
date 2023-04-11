using Kobalt.Infractions.Infrastructure.Mediator.DTOs;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Kobalt.Infractions.Data.Mediator;

public record GetAllInfractionsRequest() : IRequest<IEnumerable<InfractionDTO>>;

public class GetAllInfractionsHandler : IRequestHandler<GetAllInfractionsRequest, IEnumerable<InfractionDTO>>
{
    private readonly InfractionContext _context;

    public GetAllInfractionsHandler(InfractionContext context)
    {
        _context = context;
    }

    public async ValueTask<IEnumerable<InfractionDTO>> Handle(GetAllInfractionsRequest request, CancellationToken cancellationToken)
    {
        return await _context
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
                     .ToArrayAsync();
    }
}
