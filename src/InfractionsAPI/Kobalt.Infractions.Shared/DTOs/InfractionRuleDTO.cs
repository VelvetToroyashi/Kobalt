namespace Kobalt.Infractions.Shared.DTOs;

public record InfractionRuleDTO
(
    int Id,
    ulong GuildID,
    InfractionType ActionType,
    TimeSpan? EffectiveTimespan,
    int MatchValue,
    InfractionType MatchType,
    TimeSpan? ActionDuration
);
