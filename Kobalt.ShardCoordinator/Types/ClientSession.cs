namespace Kobalt.ShardCoordinator.Types;

public record ClientSession
(
    Guid SessionID,
    int ShardID,
    string? GatewaySessionID,
    int? Sequence,
    DateTimeOffset? AbandonedAt,
    DateTimeOffset? LastSavedAt
);
