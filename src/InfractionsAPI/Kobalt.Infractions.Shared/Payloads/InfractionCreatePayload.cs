namespace Kobalt.Infractions.Shared.Payloads;

/// <summary>
/// Represents a payload for creating a new infraction.
/// </summary>
/// <param name="Reason">The reason the payload is being created.</param>
/// <param name="UserID">The ID of the infraction's target.</param>
/// <param name="ModeratorID">The ID of the moderator responsible for the infraction.</param>
/// <param name="ReferencedID">The ID of the infraction this infraction references (e.g. </param>
/// <param name="Type"></param>
/// <param name="ExpiresAt"></param>
public record InfractionCreatePayload
(
    string Reason,
    ulong UserID,
    ulong ModeratorID,
    int? ReferencedID,
    InfractionType Type,
    DateTimeOffset? ExpiresAt
);
