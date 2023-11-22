using Kobalt.Shared.Types;
using Remora.Rest.Core;

namespace Kobalt.Bot.Data.DTOs;

public class LogChannelDTO(int Id, Snowflake ChannelID, Snowflake? WebhookID, string? WebhookToken, LogChannelType Type)
{
    public int Id { get; init; } = Id;
    public Snowflake ChannelID { get; init; } = ChannelID;
    public Snowflake? WebhookID { get; init; } = WebhookID;
    public string? WebhookToken { get; init; } = WebhookToken;
    public LogChannelType Type { get; init; } = Type;
}
