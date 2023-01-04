namespace Kobalt.ShardCoordinator.Types;

/// <summary>
/// Represents the data of a gateway session (a connection to a Discord gateway).
/// </summary>
/// <param name="SessionID">The session ID.</param>
/// <param name="Sequence">The sequence number (seq).</param>
public record GatewaySessionData
(
    string SessionID,
    int Sequence
);
