using Kobalt.Infractions.Data.Entities;
using Kobalt.Infractions.Shared;
using Kobalt.Infractions.Shared.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Remora.Results;

namespace Kobalt.Infractions.Data.MediatR;

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
) : IRequest<Result<InfractionDTO>>;

public class CreateInfractionHandler : IRequestHandler<CreateInfractionRequest, Result<InfractionDTO>>
{
    private readonly IDbContextFactory<InfractionContext> _context;

    public CreateInfractionHandler(IDbContextFactory<InfractionContext> context) => _context = context;

    public async Task<Result<InfractionDTO>> Handle(CreateInfractionRequest request, CancellationToken cancellationToken)
    {
        await using var context = await _context.CreateDbContextAsync(cancellationToken);

        if (request.Type is InfractionType.Mute or InfractionType.Ban)
        {
            var recentInfraction = await
            context
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

        if (request.Type is InfractionType.Unmute or InfractionType.Unban)
        {
            var lastMuteOrBan = await context
                                     .Infractions
                                     .Where
                                      (
                                          inf => inf.GuildID == request.GuildID &&
                                              inf.IsProcessable &&
                                              inf.Type == request.Type - 4 &&
                                              inf.UserID == request.UserID
                                      )
                                     .FirstOrDefaultAsync(cancellationToken);

            if (lastMuteOrBan is null)
            {
                return new InvalidOperationError($"The user is not {(request.Type - 4).ToString().ToLower().Replace("e", null)}ed.");
            }
        }

        var isProcessable = request.ExpiresAt is not null && request.Type is InfractionType.Ban or InfractionType.Mute;

        var infraction = new Infraction
        {
            Reason = request.Reason,
            UserID = request.UserID,
            GuildID = request.GuildID,
            ModeratorID = request.ModeratorID,
            Type = request.Type,
            IsProcessable = isProcessable,
            CreatedAt = request.CreatedAt,
            ExpiresAt = request.ExpiresAt,
            ReferencedId = request.ReferencedId
        };

        await context.Infractions.AddAsync(infraction, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

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
