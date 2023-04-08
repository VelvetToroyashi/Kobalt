using System.Text.Json.Serialization;
using Remora.Rest.Core;

namespace Kobalt.Infractions.Shared.Payloads;

public record InfractionUpdatePayload
(
    [property: JsonPropertyName("is_hidden")]
    Optional<bool> IsHidden,
    
    [property: JsonPropertyName("reason")]
    Optional<string?> Reason,
    
    [property: JsonPropertyName("expires_at")]
    Optional<DateTimeOffset?> ExpiresAt
);
