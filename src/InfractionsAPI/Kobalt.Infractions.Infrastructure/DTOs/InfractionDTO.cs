using Kobalt.Infractions.Shared;

namespace Kobalt.Infractions.Infrastructure.Mediator.DTOs;

public record InfractionDTO
(
    int Id,
    int? ReferencedId,
    bool IsHidden,
    string Reason,
    ulong UserID,
    ulong GuildID,
    ulong ModeratorID,
    InfractionType Type,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt,
    bool IsUpdated = false
);