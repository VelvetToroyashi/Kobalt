using Kobalt.Infractions.Data;
using Kobalt.Infractions.Infrastructure.Mediator.DTOs;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Kobalt.Infractions.Infrastructure.Mediator.Mediator;

// GET /infractions/guilds/{guildID}
public record GetGuildInfractionsRequest(ulong GuildID) : IRequest<IEnumerable<InfractionDTO>>;

public class GetGuildInfractionsHandler : IRequestHandler<GetGuildInfractionsRequest, IEnumerable<InfractionDTO>>
{
    private readonly InfractionContext _context;

    public GetGuildInfractionsHandler(InfractionContext context)
    {
        _context = context;
    }

    public async ValueTask<IEnumerable<InfractionDTO>> Handle(GetGuildInfractionsRequest request, CancellationToken cancellationToken)
    {
        var infractions = await _context.Infractions
            .Where(x => x.GuildID == request.GuildID)
            .ToListAsync(cancellationToken);

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