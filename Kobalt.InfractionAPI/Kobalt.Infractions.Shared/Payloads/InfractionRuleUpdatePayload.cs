using System.Text.Json.Serialization;
using Remora.Rest.Core;

namespace Kobalt.Infractions.Shared.Payloads;

public record InfractionRuleUpdatePayload
(
    [property: JsonPropertyName("action_type")]
    Optional<InfractionType> ActionType,
    
    [property: JsonPropertyName("effective_timespan")]
    Optional<TimeSpan?> MatchTimeSpan,
    
    [property: JsonPropertyName("match_value")]
    Optional<int> MatchValue,
    
    [property: JsonPropertyName("match_type")]
    Optional<InfractionType> MatchType,
    
    [property: JsonPropertyName("action_duration")]
    Optional<TimeSpan?> ActionDuration
);
