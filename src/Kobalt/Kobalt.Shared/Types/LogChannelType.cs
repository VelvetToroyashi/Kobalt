using Remora.Discord.Commands.Attributes;

namespace Kobalt.Shared.Types;

/// <summary>
/// Represents the type of logging a channel is used for.
/// </summary>
[Flags]
public enum LogChannelType : ulong
{
    /// <summary>
    /// Any issues that arise from Kobalt.
    /// </summary>
    BotAlert = 1 << 0,
    
    /// <summary>
    /// When a case is created.
    /// </summary>
    CaseCreate = 1 << 1,
    
    /// <summary>
    /// When a case is updated.
    /// </summary>
    CaseUpdate = 1 << 2,
    
    /// <summary>
    /// When a member joins.
    /// </summary>
    MemberJoin = 1 << 3,
    
    /// <summary>
    /// When a member leaves.
    /// </summary>
    MemberLeave = 1 << 4,
    
    /// <summary>
    /// When raid mode is enabled.
    /// </summary>
    RaidModeEnabled = 1 << 5,
    
    /// <summary>
    /// When raid mode is disabled.
    /// </summary>
    RaidModeDisabled = 1 << 6,
    
    /// <summary>
    /// When a message is deleted or edited.
    /// </summary>
    MessageUpdates = 1 << 7,
}
