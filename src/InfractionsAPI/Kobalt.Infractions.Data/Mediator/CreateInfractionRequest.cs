using Kobalt.Infractions.Data;
using Kobalt.Infractions.Data.Entities;
using Kobalt.Infractions.Infrastructure.Mediator.DTOs;
using Kobalt.Infractions.Shared;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Kobalt.Infractions.Infrastructure.Mediator;

public record CreateInfractionRequest
(
    string Reason,
    ulong UserID,
    ulong GuildID,
    ulong ModeratorID,
    InfractionType Type,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt,
    int? ReferencedId = null
) : IRequest<InfractionDTO>;

public class CreateInfractionHandler : IRequestHandler<CreateInfractionRequest, InfractionDTO>
{
    private readonly InfractionContext _context;

    public CreateInfractionHandler(InfractionContext context)
    {
        _context = context;
    }

    public async ValueTask<InfractionDTO> Handle(CreateInfractionRequest request, CancellationToken cancellationToken)
    {
        if (request.Type is InfractionType.Mute or InfractionType.Ban)
        {
            var recentInfraction = await 
            _context
            .Infractions
            .FirstOrDefaultAsync
            (
                x =>
                x.UserID == request.UserID &&
                x.GuildID == request.GuildID &&
                x.IsProcessable &&
                x.Type == request.Type,
                cancellationToken
            );
            
            if (recentInfraction is not null)
            {
                recentInfraction.ExpiresAt = request.ExpiresAt;

                return new InfractionDTO
                (
                    recentInfraction.Id,
                    recentInfraction.ReferencedId,
                    recentInfraction.IsHidden,
                    recentInfraction.Reason,
                    recentInfraction.UserID,
                    recentInfraction.GuildID,
                    recentInfraction.ModeratorID,
                    recentInfraction.Type,
                    recentInfraction.CreatedAt,
                    recentInfraction.ExpiresAt
                );
            }
        }

        var infraction = new Infraction
        {
            Reason = request.Reason,
            UserID = request.UserID,
            GuildID = request.GuildID,
            ModeratorID = request.ModeratorID,
            Type = request.Type,
            CreatedAt = request.CreatedAt,
            ExpiresAt = request.ExpiresAt,
            ReferencedId = request.ReferencedId
        };

        await _context.Infractions.AddAsync(infraction, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new InfractionDTO
        (
            infraction.Id,
            infraction.ReferencedId,
            infraction.IsHidden,
            infraction.Reason,
            infraction.UserID,
            infraction.GuildID,
            infraction.ModeratorID,
            infraction.Type,
            infraction.CreatedAt,
            infraction.ExpiresAt
        );
    }
}
