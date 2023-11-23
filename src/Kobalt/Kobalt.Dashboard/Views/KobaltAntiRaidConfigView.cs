using Kobalt.Bot.Data.DTOs;
using Remora.Discord.API.Abstractions.Objects;
using Singulink.Enums;

namespace Kobalt.Dashboard.Views;

public class KobaltAntiRaidConfigView(GuildAntiRaidConfigDTO dto)
{
    public bool IsEnabled { get; set; } = dto.IsEnabled;
    public int BaseJoinScore { get; set; } = dto.BaseJoinScore;
    public TimeSpan? MiniumAccountAgeBypass { get; set; } = dto.MiniumAccountAgeBypass;
    public List<UserFlags> AccountFlagsBypass { get; set; } = dto.AccountFlagsBypass?.SplitFlags().ToList();
    public int JoinVelocityScore { get; set; } = dto.JoinVelocityScore;
    public int MinimumAgeScore { get; set; } = dto.MinimumAgeScore;
    public int NoAvatarScore { get; set; } = dto.NoAvatarScore;
    public int SuspiciousInviteScore { get; set; } = dto.SuspiciousInviteScore;
    public int ThreatScoreThreshold { get; set; } = dto.ThreatScoreThreshold;
    public TimeSpan AntiRaidCooldownPeriod { get; set; } = dto.AntiRaidCooldownPeriod;
    public TimeSpan LastJoinBufferPeriod { get; set; } = dto.LastJoinBufferPeriod;
    public TimeSpan MinimumAccountAge { get; set; } = dto.MinimumAccountAge;

    public GuildAntiRaidConfigDTO ToDTO()
    {
        return new GuildAntiRaidConfigDTO
        (
            IsEnabled,
            BaseJoinScore,
            MiniumAccountAgeBypass,
            AccountFlagsBypass.Any() ? default(UserFlags).SetFlags(AccountFlagsBypass) : null,
            JoinVelocityScore,
            MinimumAgeScore,
            NoAvatarScore,
            SuspiciousInviteScore,
            ThreatScoreThreshold,
            AntiRaidCooldownPeriod,
            LastJoinBufferPeriod,
            MinimumAccountAge
        );
    }
}