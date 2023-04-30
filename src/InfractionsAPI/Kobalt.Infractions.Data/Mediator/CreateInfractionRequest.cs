﻿using Kobalt.Infractions.Data;
using Kobalt.Infractions.Data.Entities;
using Kobalt.Infractions.Infrastructure.Mediator.DTOs;
using Kobalt.Infractions.Shared;
using MediatR;
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
    private readonly IDbContextFactory<InfractionContext> _context;

    public CreateInfractionHandler(IDbContextFactory<InfractionContext> context) => _context = context;

    public async Task<InfractionDTO> Handle(CreateInfractionRequest request, CancellationToken cancellationToken)
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
