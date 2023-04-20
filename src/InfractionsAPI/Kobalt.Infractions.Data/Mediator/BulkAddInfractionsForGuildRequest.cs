using Kobalt.Infractions.Data;
using Kobalt.Infractions.Data.Entities;
using Kobalt.Infractions.Infrastructure.Mediator.DTOs;
using Kobalt.Infractions.Shared.Payloads;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Kobalt.Infractions.Infrastructure.Mediator.Mediator;

// PUT /infractions/guilds/{guildID}
public record BulkAddInfractionsForGuildRequest(ulong GuildID, IReadOnlyList<InfractionCreatePayload> Infractions)
: IRequest<IEnumerable<InfractionDTO>>;

public class BulkAddInfractionsForGuildHandler : IRequestHandler<BulkAddInfractionsForGuildRequest, IEnumerable<InfractionDTO>>
{
    private readonly IDbContextFactory<InfractionContext> _contextFactory;

    public BulkAddInfractionsForGuildHandler(IDbContextFactory<InfractionContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async ValueTask<IEnumerable<InfractionDTO>> Handle(BulkAddInfractionsForGuildRequest request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var infractions = request
                          .Infractions
                          .Select(x => new Infraction
                          {
                              GuildID = request.GuildID,
                              UserID = x.UserID,
                              Reason = x.Reason,
                              Type = x.Type,
                              CreatedAt = now,
                              ExpiresAt = x.ExpiresAt,
                              ModeratorID = x.ModeratorID
                          })
                          .ToArray();
        
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        context.Infractions.AddRange(infractions);
        await context.SaveChangesAsync(cancellationToken);

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

