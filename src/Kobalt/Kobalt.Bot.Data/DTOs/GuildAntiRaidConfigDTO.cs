using Remora.Discord.API.Abstractions.Objects;

namespace Kobalt.Bot.Data.DTOs;

/// <summary>
/// Represents the anti-raid configuration for a guild.
/// </summary>
/// <param name="IsEnabled">Whether anti-raid is enabled.</param>
/// <param name="BaseJoinScore">The base score applied to all joins.</param>
/// <param name="MiniumAccountAgeBypass">The minimum age of an account to bypass anti-raid, if any.</param>
/// <param name="AccountFlagsBypass">Account flags to bypass anti-raid, if any.</param>
/// <param name="JoinVelocityScore">The score granted for exceeding the join velocity check.</param>
/// <param name="MinimumAgeScore">The score granted for an account being too young (determined by <see cref="MinimumAccountAge"/>).</param>
/// <param name="NoAvatarScore">The score granted for an account not having an avatar.</param>
/// <param name="SuspiciousInviteScore">The score granted for an account joining from a suspicious invite.</param>
/// <param name="ThreatScoreThreshold">The score at which a raid is considered.</param>
/// <param name="AntiRaidCooldownPeriod">The period of time after the last join at which raids begin to decay.</param>
/// <param name="LastJoinBufferPeriod">The period in which two consecutive joins are considered suspicious.</param>
/// <param name="MinimumAccountAge">The minimum age of an account. Anything newer is considered suspicious.</param>
public class GuildAntiRaidConfigDTO
(
    bool IsEnabled,
    int BaseJoinScore,
    TimeSpan? MiniumAccountAgeBypass,
    UserFlags? AccountFlagsBypass,
    int JoinVelocityScore,
    int MinimumAgeScore,
    int NoAvatarScore,
    int SuspiciousInviteScore,
    int ThreatScoreThreshold,
    TimeSpan AntiRaidCooldownPeriod,
    TimeSpan LastJoinBufferPeriod,
    TimeSpan MinimumAccountAge
)
{
    /// <summary>Whether anti-raid is enabled.</summary>
    public bool IsEnabled { get; set; } = IsEnabled;

    /// <summary>The base score applied to all joins.</summary>
    public int BaseJoinScore { get; set; } = BaseJoinScore;

    /// <summary>The minimum age of an account to bypass anti-raid, if any.</summary>
    public TimeSpan? MiniumAccountAgeBypass { get; set; } = MiniumAccountAgeBypass;

    /// <summary>Account flags to bypass anti-raid, if any.</summary>
    public UserFlags? AccountFlagsBypass { get; set; } = AccountFlagsBypass;

    /// <summary>The score granted for exceeding the join velocity check.</summary>
    public int JoinVelocityScore { get; set; } = JoinVelocityScore;

    /// <summary>The score granted for an account being too young (determined by <see cref="MinimumAccountAge"/>).</summary>
    public int MinimumAgeScore { get; set; } = MinimumAgeScore;

    /// <summary>The score granted for an account not having an avatar.</summary>
    public int NoAvatarScore { get; set; } = NoAvatarScore;

    /// <summary>The score granted for an account joining from a suspicious invite.</summary>
    public int SuspiciousInviteScore { get; set; } = SuspiciousInviteScore;

    /// <summary>The score at which a raid is considered.</summary>
    public int ThreatScoreThreshold { get; set; } = ThreatScoreThreshold;

    /// <summary>The period of time after the last join at which raids begin to decay.</summary>
    public TimeSpan AntiRaidCooldownPeriod { get; set; } = AntiRaidCooldownPeriod;

    /// <summary>The period in which two consecutive joins are considered suspicious.</summary>
    public TimeSpan LastJoinBufferPeriod { get; set; } = LastJoinBufferPeriod;

    /// <summary>The minimum age of an account. Anything newer is considered suspicious.</summary>
    public TimeSpan MinimumAccountAge { get; set; } = MinimumAccountAge;
}
