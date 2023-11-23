using Kobalt.Bot.Data.DTOs;

namespace Kobalt.Dashboard.Views;

public class KobaltAutoModView(GuildAutoModConfigDTO dto)
{
    public int? PushToTalkThreshold { get; set; } = dto.PushToTalkThreshold;

    public GuildAutoModConfigDTO ToDTO() => new(PushToTalkThreshold);
}