using Kobalt.Infractions.Shared;
using MassTransit.Futures.Contracts;

namespace Kobalt.Infractions.Data.Entities;

/// <summary>
/// Represents a rule in a set of rules for a guild for applying infractions.
/// <para>
/// When an infraction is applied to a user, two things happen:
/// - Firstly, the infraction is applied (e.g. a warning, a mute, a ban, etc.)
/// - Secondly, the list of infractions for the guild is requested, and matched against.
///
/// Matching infractions looks through the list of rules, attempting to match the infraction
/// type (<see cref="MatchType"/>), and the number of infractions (<see cref="MatchValue"/>).
///
/// If the infraction type matches, and the number of infractions is greater than or equal to
/// the number of infractions specified in the rule, then the rule is applied (determined by
/// <see cref="ActionType"/>).
/// </para>
/// </summary>
public class InfractionRule
{
    /// <summary>
    /// The unique ID of the rule.
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// The user-friendly name of this rule.
    /// </summary>
    public string RuleName { get; set; }
    
    /// <summary>
    /// The ID of the guild that this rule applies to.
    /// </summary>
    public required ulong GuildID { get; set; }
    
    /// <summary>
    /// The action type that this rule applies
    /// </summary>
    public required InfractionType ActionType { get; set; }
    
    /// <summary>
    /// The time this rule applies for. (e.g. 3 mutes in a week). 
    /// </summary>
    public required TimeSpan? MatchTimeSpan { get; set; }
    
    /// <summary>
    /// How many of <see cref="MatchType"/> are required to trigger this rule.
    /// </summary>
    public required int MatchValue { get; set; }
    
    /// <summary>
    /// The type of infraction that triggers this rule.
    /// </summary>
    public required InfractionType MatchType { get; set; }
    
    /// <summary>
    /// The duration this rule will apply (e.g. mute for an hour).
    /// </summary>
    public required TimeSpan? ActionDuration { get; set; }
}

