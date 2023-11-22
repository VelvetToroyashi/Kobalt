using Kobalt.Infractions.Shared;

namespace Kobalt.Bot.Data.DTOs;

public class GuildAntiPhishingConfigDTO(int Id, bool ScanLinks, bool ScanUsers, InfractionType DetectionAction)
{
    public int Id { get; init; } = Id;
    public bool ScanLinks { get; init; } = ScanLinks;
    public bool ScanUsers { get; init; } = ScanUsers;
    public InfractionType DetectionAction { get; init; } = DetectionAction;
}