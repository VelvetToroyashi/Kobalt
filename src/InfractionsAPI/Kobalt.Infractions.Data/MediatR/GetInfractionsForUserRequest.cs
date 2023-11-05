using Kobalt.Infractions.Shared;
using Kobalt.Infractions.Shared.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kobalt.Infractions.Data.MediatR;

// GET /infractions/guilds/{guildID}/users/{userID}
public record GetInfractionsForUserRequest(ulong GuildID, ulong UserID, bool IncludePardons) : IRequest<IEnumerable<InfractionDTO>>;

public class GetInfractionsForUserHandler : IRequestHandler<GetInfractionsForUserRequest, IEnumerable<InfractionDTO>>
{
    private readonly IDbContextFactory<InfractionContext> _context;

    public GetInfractionsForUserHandler(IDbContextFactory<InfractionContext> context) => _context = context;

    public async Task<IEnumerable<InfractionDTO>> Handle(GetInfractionsForUserRequest request, CancellationToken cancellationToken)
    {
        await using var context = await _context.CreateDbContextAsync(cancellationToken);

        var infractionQuery = context.Infractions.Where(x => x.GuildID == request.GuildID && x.UserID == request.UserID);

        if (!request.IncludePardons)
        {
            infractionQuery = infractionQuery.Where
            (
                inf => inf.Type != InfractionType.Unban && 
                       inf.Type != InfractionType.Unmute && 
                       inf.Type != InfractionType.Pardon
            );
        }
        
        var infractions = await infractionQuery
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
