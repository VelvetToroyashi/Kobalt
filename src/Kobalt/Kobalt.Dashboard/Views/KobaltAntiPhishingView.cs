using Kobalt.Bot.Data.DTOs;
using Kobalt.Infractions.Shared;

namespace Kobalt.Dashboard.Views;

public class KobaltAntiPhishingView(GuildAntiPhishingConfigDTO dto)
{
    public bool ScanLinks { get; set; } = dto.ScanLinks;
    public bool ScanUsers { get; set; } = dto.ScanUsers;
    public InfractionType DetectionAction { get; set; } = dto.DetectionAction;

    public GuildAntiPhishingConfigDTO ToDTO()
    {
        return new GuildAntiPhishingConfigDTO(dto.Id, ScanLinks, ScanUsers, DetectionAction);
    }
}