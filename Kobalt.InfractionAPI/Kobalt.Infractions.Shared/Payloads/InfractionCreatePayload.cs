using System.Text.Json.Serialization;

namespace Kobalt.Infractions.Shared.Payloads;

public record InfractionCreatePayload
(
    [property: JsonPropertyName("id")]
    int Id,
    
    [property: JsonPropertyName("is_hidden")]
    bool IsHidden,
    
    [property: JsonPropertyName("reason")]
    string Reason,
    
    [property: JsonPropertyName("user_id")]
    ulong UserID,
    
    [property: JsonPropertyName("moderator_id")]
    ulong ModeratorID,
    
    [property: JsonPropertyName("type")]
    InfractionType Type,
    
    [property: JsonPropertyName("created_at")]
    DateTimeOffset CreatedAt,
    
    [property: JsonPropertyName("expires_at")]
    DateTimeOffset? ExpiresAt,
    
    [property: JsonPropertyName("is_updated")]
    bool IsUpdated = false
);
