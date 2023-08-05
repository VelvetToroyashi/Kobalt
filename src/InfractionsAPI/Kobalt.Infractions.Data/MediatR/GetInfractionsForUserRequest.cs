using Kobalt.Infractions.Shared.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kobalt.Infractions.Data.MediatR;

// GET /infractions/guilds/{guildID}/users/{userID}
public record GetInfractionsForUserRequest(ulong GuildID, ulong UserID) : IRequest<IEnumerable<InfractionDTO>>;

public class GetInfractionsForUserHandler : IRequestHandler<GetInfractionsForUserRequest, IEnumerable<InfractionDTO>>
{
    private readonly IDbContextFactory<InfractionContext> _context;

    public GetInfractionsForUserHandler(IDbContextFactory<InfractionContext> context) => _context = context;

    public async Task<IEnumerable<InfractionDTO>> Handle(GetInfractionsForUserRequest request, CancellationToken cancellationToken)
    {
        await using var context = await _context.CreateDbContextAsync(cancellationToken);

        var infractions = await context.Infractions
            .Where(x => x.GuildID == request.GuildID && x.UserID == request.UserID)
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
