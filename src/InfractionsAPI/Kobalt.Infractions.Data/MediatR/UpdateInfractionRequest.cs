﻿using Kobalt.Infractions.Shared.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Infractions.Data.MediatR;

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
    private readonly IDbContextFactory<InfractionContext> _context;

    public UpdateInfractionRequestHandler(IDbContextFactory<InfractionContext> context) => _context = context;

    public async Task<Result<InfractionDTO>> Handle(UpdateInfractionRequest request, CancellationToken ct = default)
    {
        await using var context = await _context.CreateDbContextAsync(ct);

        var infraction = await context.Infractions.FindAsync(request.Id, ct);

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

        await context.SaveChangesAsync(ct);

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
