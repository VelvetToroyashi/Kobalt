using Remora.Discord.Commands.Attributes;

namespace Kobalt.Shared.Types;

/// <summary>
/// Represents the type of logging a channel is used for.
/// </summary>
public enum LogChannelType : ulong
{
    /// <summary>
    /// Any issues that arise from Kobalt.
    /// </summary>
    BotAlert = 1,
    
    /// <summary>
    /// When a case is created.
    /// </summary>
    CaseCreate,
    
    /// <summary>
    /// When a case is updated.
    /// </summary>
    CaseUpdate,
    
    /// <summary>
    /// When a member joins.
    /// </summary>
    MemberJoin,
    
    /// <summary>
    /// When a member leaves.
    /// </summary>
    MemberLeave,
    
    /// <summary>
    /// When raid mode is enabled.
    /// </summary>
    RaidModeEnabled,
    
    /// <summary>
    /// When raid mode is disabled.
    /// </summary>
    RaidModeDisabled,
    
    /// <summary>
    /// When a message is deleted or edited.
    /// </summary>
    MessageUpdates,
    
    [ExcludeFromChoices]
    Placeholder1,
    
    [ExcludeFromChoices]
    Placeholder2,
    
    [ExcludeFromChoices]
    Placeholder3,
    
    [ExcludeFromChoices]
    Placeholder4,
}
