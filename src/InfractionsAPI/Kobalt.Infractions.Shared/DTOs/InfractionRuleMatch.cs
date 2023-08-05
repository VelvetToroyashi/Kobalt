namespace Kobalt.Infractions.Shared.DTOs;

/// <summary>
/// Represents a match on an infraction rule.
/// </summary>
/// <param name="Type">The type of the infraction to be created</param>
/// <param name="Duration"></param>
public record InfractionRuleMatch(InfractionType Type, TimeSpan? Duration);
