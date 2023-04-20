using Remora.Commands.Conditions;

namespace Kobalt.Shared.Conditions;

/// <summary>
/// The target of the hierarchy check.
/// </summary>
public enum HierarchyTarget
{
    /// <summary>
    /// The bot.
    /// </summary>
    /// <remarks>
    /// Only applicable if <see cref="EnsureHierarchyAttribute"/> is applied to a parameter.
    /// </remarks>
    Self,
    
    /// <summary>
    /// The invoker of the command.
    /// </summary>
    Invoker
}

public enum HierarchyLevel
{
    /// <summary>
    /// The hierarchy level is equal or greater than the target.
    /// </summary>
    Higher,
    
    /// <summary>
    /// The hierarchy level is equal or lower than the target.
    /// </summary>
    Lower,
    
    /// <summary>
    /// Only used internally. Using this will result in an error.
    /// </summary>
    Equal
}

/// <summary>
/// Ensures that a given hierarchy exists. e.g. if the target is <see cref="HierarchyTarget.Invoker"/>,
/// and the level is <see cref="HierarchyLevel.Higher"/>, the invoker must have a higher hierarchy level than the bot.
/// </summary>

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter, AllowMultiple = true)]
public class EnsureHierarchyAttribute : ConditionAttribute
{
    public HierarchyTarget Target { get; }
    public HierarchyLevel Level { get; }
    
    public EnsureHierarchyAttribute(HierarchyTarget target, HierarchyLevel level)
    {
        Target = target;
        Level = level;
    }
}
