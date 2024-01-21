using Kobalt.Infractions.Shared.DTOs;
using Remora.Rest.Core;

namespace Kobalt.Infractions.Shared.Payloads;

/// <summary>
/// Represents the response payload for an infraction.
/// </summary>
/// <param name="Id">The ID of the infraction.</param>
/// <param name="Reason">The reason for the infraction.</param>
/// <param name="UserID">The ID of the user who received the infraction.</param>
/// <param name="ModeratorID">The ID of the moderator who issued the infraction.</param>
/// <param name="GuildID">The ID of the guild where the infraction was issued.</param>
/// <param name="Type">The type of the infraction.</param>
/// <param name="CreatedAt">The date and time when the infraction was created.</param>
/// <param name="ExpiresAt">The date and time when the infraction expires.</param>
/// <param name="IsUpdated">A value indicating whether the infraction has been updated.</param>
/// <param name="AdditionalInfractions">An optional list of additional infractions.</param>
public record InfractionResponsePayload
(
    int Id,
    string Reason,
    ulong UserID,
    ulong ModeratorID,
    ulong GuildID,
    InfractionType Type,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt,
    bool IsUpdated = false,
    int? ReferencedId = default,
    Optional<IReadOnlyList<InfractionDTO>> AdditionalInfractions = default
)
{
    public static InfractionResponsePayload FromDTO
    (
        InfractionDTO dto, 
        bool isUpdated, 
        Optional<IReadOnlyList<InfractionDTO>> additionalInfractions = default
    )
    {
        return new InfractionResponsePayload
        (
            dto.Id,
            dto.Reason,
            dto.UserID,
            dto.ModeratorID,
            dto.GuildID,
            dto.Type,
            dto.CreatedAt,
            dto.ExpiresAt,
            isUpdated,
            dto.ReferencedId,
            additionalInfractions
        );
    }
}

