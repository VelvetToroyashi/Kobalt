using Kobalt.Infractions.Shared;

namespace Kobalt.Infractions.Infrastructure.Mediator.DTOs;

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
