using Kobalt.Infractions.Infrastructure.Mediator.DTOs;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Remora.Results;

namespace Kobalt.Infractions.Data.Mediator;

// GET /infractions/guilds/{guildID}/{id}
/// <summary>
/// Requests an infraction from the database for a specific guild.
/// </summary>
/// <param name="InfractionID">The ID of the infraction to query.</param>
/// <param name="GuildID">The ID of the guild the infraction is being requested for.</param>
public record GetGuildInfractionRequest(int InfractionID, ulong GuildID) : IRequest<Result<InfractionDTO>>;

public class GetGuildInfractionHandler : IRequestHandler<GetGuildInfractionRequest, Result<InfractionDTO>>
{
    private readonly IDbContextFactory<InfractionContext> _context;
    
    public GetGuildInfractionHandler(IDbContextFactory<InfractionContext> context)
    {
        _context = context;
    }

    public async ValueTask<Result<InfractionDTO>> Handle(GetGuildInfractionRequest request, CancellationToken cancellationToken)
    {
        await using var context = await _context.CreateDbContextAsync(cancellationToken);

        var infraction = await context.Infractions.FindAsync(request.InfractionID);

        if (infraction is null)
        {
            return new NotFoundError();
        }

        if (infraction.GuildID != request.GuildID)
        {
            return new NotFoundError();
        }

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