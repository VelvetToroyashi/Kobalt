using Kobalt.Infractions.Data;
using Kobalt.Infractions.Infrastructure.Mediator.DTOs;
using Mediator;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Infractions.Infrastructure.Mediator;

// PATCH /infractions/guilds/{guildID}/{ID}
public record UpdateInfractionRequest
(
    int Id,
    ulong GuildID,
    Optional<bool> IsHidden,
    Optional<string?> Reason,
    Optional<DateTimeOffset?> ExpiresAt,
    Optional<bool> IsProcessable = default
)
: IRequest<Result<InfractionDTO>>;

public class UpdateInfractionRequestHandler : IRequestHandler<UpdateInfractionRequest, Result<InfractionDTO>>
{
    private readonly InfractionContext _context;

    public UpdateInfractionRequestHandler(InfractionContext context)
    {
        _context = context;
    }

    public async ValueTask<Result<InfractionDTO>> Handle(UpdateInfractionRequest request, CancellationToken ct = default)
    {
        var infraction = await _context.Infractions.FindAsync(request.Id, ct);
        
        if (infraction is null)
        {
            return new NotFoundError("Infraction not found");
        }
        
        if (infraction.GuildID != request.GuildID)
        {
            return new NotFoundError("Infraction not found");
        }
        
        if (!request.IsHidden.HasValue && !request.Reason.HasValue && !request.ExpiresAt.HasValue && !request.IsProcessable.HasValue)
        {
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

        if (request.IsHidden.IsDefined(out var isHidden))
        {
            infraction.IsHidden = isHidden;
        }

        if (request.Reason.IsDefined(out var reason))
        {
            infraction.Reason = reason;
        }

        if (request.ExpiresAt.IsDefined(out var expiresAt))
        {
            infraction.ExpiresAt = expiresAt;
        }

        if (request.IsProcessable.IsDefined(out var isProcessable))
        {
            infraction.IsProcessable = isProcessable;
        }

        await _context.SaveChangesAsync(ct);

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