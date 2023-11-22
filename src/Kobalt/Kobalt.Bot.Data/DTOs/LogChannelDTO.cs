using Kobalt.Shared.Types;
using Remora.Rest.Core;

namespace Kobalt.Bot.Data.DTOs;

public class LogChannelDTO(int Id, Snowflake ChannelID, Snowflake? WebhookID, string? WebhookToken, LogChannelType Type)
{
    public int Id { get; set; } = Id;
    public Snowflake ChannelID { get; set; } = ChannelID;
    public Snowflake? WebhookID { get; set; } = WebhookID;
    public string? WebhookToken { get; set; } = WebhookToken;
    public LogChannelType Type { get; set; } = Type;
}
