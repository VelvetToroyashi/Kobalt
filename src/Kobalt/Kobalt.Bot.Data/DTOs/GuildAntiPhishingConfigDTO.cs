using Kobalt.Infractions.Shared;

namespace Kobalt.Bot.Data.DTOs;

public record GuildAntiPhishingConfigDTO(int Id, bool ScanLinks, bool ScanUsers, InfractionType DetectionAction);