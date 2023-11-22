using Kobalt.Infractions.Shared;

namespace Kobalt.Bot.Data.DTOs;

public class GuildAntiPhishingConfigDTO(int Id, bool ScanLinks, bool ScanUsers, InfractionType DetectionAction)
{
    public int Id { get; set; } = Id;
    public bool ScanLinks { get; set; } = ScanLinks;
    public bool ScanUsers { get; set; } = ScanUsers;
    public InfractionType DetectionAction { get; set; } = DetectionAction;
}