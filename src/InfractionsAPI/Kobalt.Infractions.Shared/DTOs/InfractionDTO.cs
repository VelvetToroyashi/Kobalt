using Kobalt.Infractions.Shared;

namespace Kobalt.Infractions.Infrastructure.Mediator.DTOs;

/// <summary>
/// Represents an infraction in the system.
/// </summary>
/// <param name="Id">The ID of the infraction.</param>
/// <param name="ReferencedId">The ID of the referenced infraction, if applicable.</param>
/// <param name="IsHidden">Whether this infraction is hidden.</param>
/// <param name="Reason">The reason for this infraction.</param>
/// <param name="UserID">The ID of the target user.</param>
/// <param name="GuildID">The ID of the guild this infraction was created in.</param>
/// <param name="ModeratorID">The ID of the moderator that created this infraction.</param>
/// <param name="Type">The type of this infraction.</param>
/// <param name="CreatedAt">The time this infraction was created.</param>
/// <param name="ExpiresAt">When this infraction expires, if applicable.</param>
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
    DateTimeOffset? ExpiresAt
);