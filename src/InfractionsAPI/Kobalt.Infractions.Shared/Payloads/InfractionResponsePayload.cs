using System.Text.Json.Serialization;

namespace Kobalt.Infractions.Shared.Payloads;

public record InfractionResponsePayload
(
    int Id,
    bool IsHidden,
    string Reason,
    ulong UserID,
    ulong ModeratorID,
    
    [property: JsonPropertyName("guild_id")]
    ulong GuildID,

    InfractionType Type,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt,
    bool IsUpdated = false
) 
: InfractionCreatePayload
(
    Id,
    IsHidden,
    Reason,
    UserID,
    ModeratorID,
    Type,
    CreatedAt,
    ExpiresAt
);

