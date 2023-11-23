using Kobalt.Bot.Data.DTOs;
using Kobalt.Shared.Types;
using Remora.Rest.Core;
using Singulink.Enums;

namespace Kobalt.Dashboard.Views;

public class KobaltLoggingConfigView(LogChannelDTO channel)
{
    public Snowflake ChannelID { get; init; } = channel.ChannelID;
    public Snowflake? WebhookID { get; init; } = channel.WebhookID;
    public string? WebhookToken { get; init; } = channel.WebhookToken;
    public List<LogChannelType> Types { get; set; } = channel.Type.SplitFlags().ToList();

    public LogChannelDTO ToDTO() 
        => new(ChannelID, WebhookID, WebhookToken, default(LogChannelType).SetFlags(Types));
}