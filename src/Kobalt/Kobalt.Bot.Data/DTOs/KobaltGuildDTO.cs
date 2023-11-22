using Kobalt.Bot.Data.Entities;

namespace Kobalt.Bot.Data.DTOs;

public record KobaltGuildDTO
(
    GuildAutoModConfigDTO AutoModConfig,
    GuildAntiRaidConfigDTO AntiRaidConfig,
    GuildAntiPhishingConfigDTO AntiPhishingConfig,
    IReadOnlyList<LogChannelDTO> LogChannels
)
{
    public static KobaltGuildDTO FromEntity(KobaltGuild entity)
    {
        var automodConfig = new GuildAutoModConfigDTO(entity.AutoModConfig.PushToTalkThreshold);

        var antiRaidConfig = new GuildAntiRaidConfigDTO
        (
            entity.AntiRaidConfig.IsEnabled,
            entity.AntiRaidConfig.BaseJoinScore,
            entity.AntiRaidConfig.MiniumAccountAgeBypass,
            entity.AntiRaidConfig.AccountFlagsBypass,
            entity.AntiRaidConfig.JoinVelocityScore,
            entity.AntiRaidConfig.MinimumAgeScore,
            entity.AntiRaidConfig.NoAvatarScore,
            entity.AntiRaidConfig.SuspiciousInviteScore,
            entity.AntiRaidConfig.ThreatScoreThreshold,
            entity.AntiRaidConfig.AntiRaidCooldownPeriod,
            entity.AntiRaidConfig.LastJoinBufferPeriod,
            entity.AntiRaidConfig.MinimumAccountAge
        );
        
        var antiPhishingConfig = new GuildAntiPhishingConfigDTO
        (
            entity.PhishingConfig.Id,
            entity.PhishingConfig.ScanLinks,
            entity.PhishingConfig.ScanUsers,
            entity.PhishingConfig.DetectionAction
        );
        
        var logChannels = entity.LogChannels.Select(l => new LogChannelDTO(l.Id, l.ChannelID, l.WebhookID, l.WebhookToken, l.Type)).ToArray();
        
        return new KobaltGuildDTO(automodConfig, antiRaidConfig, antiPhishingConfig, logChannels);
    }
}