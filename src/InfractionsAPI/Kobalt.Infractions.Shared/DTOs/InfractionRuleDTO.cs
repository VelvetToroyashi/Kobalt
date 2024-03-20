namespace Kobalt.Infractions.Shared.DTOs;

/// <summary>
/// Represents an infraction rule within the system.
/// </summary>
/// <param name="Id">The unique identifier for the infraction rule.</param>
/// <param name="GuildID">The unique identifier for the guild associated with the infraction rule.</param>
/// <param name="RuleName">The name of the infraction rule.</param>
/// <param name="ActionType">The type of infraction that will be applied when the rule is triggered.</param>
/// <param name="EffectiveTimespan">The timespan during which the rule is effective. If null, the rule is always effective.</param>
/// <param name="MatchValue">The number of infractions of the specified type that must occur for the rule to be triggered.</param>
/// <param name="MatchType">The type of infraction that the rule is looking for.</param>
/// <param name="ActionDuration">The duration for which the action will be applied when the rule is triggered. If null, the action is permanent.</param>
public record InfractionRuleDTO
(
    int Id,
    ulong GuildID,
    string RuleName,
    InfractionType ActionType,
    TimeSpan? EffectiveTimespan,
    int MatchValue,
    InfractionType MatchType,
    TimeSpan? ActionDuration
);