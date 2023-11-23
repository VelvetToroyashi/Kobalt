using Kobalt.Bot.Data.DTOs;

namespace Kobalt.Dashboard.Views;

public class KobaltGuildView(KobaltGuildDTO dto)
{
    public KobaltAutoModView AutoMod { get; init; } = new KobaltAutoModView(dto.AutoModConfig);
    public KobaltAntiPhishingView AntiPhishing { get; init; } = new KobaltAntiPhishingView(dto.AntiPhishingConfig);
    public KobaltAntiRaidConfigView AntiRaid { get; init; } = new KobaltAntiRaidConfigView(dto.AntiRaidConfig);
    public List<KobaltLoggingConfigView> Logging { get; init; } = dto.LogChannels.Select(x => new KobaltLoggingConfigView(x)).ToList();
    
    public KobaltGuildDTO ToDTO() => new
    (
        AutoMod.ToDTO(),
        AntiRaid.ToDTO(),
        AntiPhishing.ToDTO(),
        Logging.Select(x => x.ToDTO()).ToList()
    );
}