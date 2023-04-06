using System.Text.Json.Serialization;

namespace Kobalt.Infrastructure.Types.ShardCoordinator;

public record ServerGatewaySessionData
(
    [property: JsonPropertyName("id")]
    Guid SessionID,
    
    [property: JsonPropertyName("shard_id")]
    int ShardID,
    
    [property: JsonPropertyName("seq")]
    int Sequence,
    
    [property: JsonPropertyName("gateway_session_id")]
    string? GatewaySessionID
);

public record ClientGatewaySessionData
(
    [property: JsonPropertyName("seq")]
    int Sequence,
    
    [property: JsonPropertyName("gateway_session_id")]
    string? GatewaySessionID
);