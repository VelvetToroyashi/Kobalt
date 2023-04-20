namespace Kobalt.Infractions.Shared.Payloads;

public record InfractionResponsePayload
(
    int Id,
    bool IsHidden,
    string Reason,
    ulong UserID,
    ulong ModeratorID,
    ulong GuildID,
    InfractionType Type,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt,
    bool IsUpdated = false
);

