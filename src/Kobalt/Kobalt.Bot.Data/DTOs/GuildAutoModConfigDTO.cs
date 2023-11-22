namespace Kobalt.Bot.Data.DTOs;

public class GuildAutoModConfigDTO(int? PushToTalkThreshold)
{
    public int? PushToTalkThreshold { get; init; } = PushToTalkThreshold;
}