using Kobalt.Shared.Types;
using Remora.Rest.Core;

namespace Kobalt.Bot.Data.DTOs;

public class LogChannelDTO(Snowflake ChannelID, Snowflake? WebhookID, string? WebhookToken, LogChannelType Type)
{
    public Snowflake ChannelID { get; init; } = ChannelID;
    public Snowflake? WebhookID { get; init; } = WebhookID;
    public string? WebhookToken { get; init; } = WebhookToken;
    public LogChannelType Type { get; init; } = Type;
}
