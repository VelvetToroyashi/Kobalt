using Kobalt.Shared.Types;
using Remora.Rest.Core;

namespace Kobalt.Bot.Data.DTOs;

public record LogChannelDTO(int Id, Snowflake ChannelID, Snowflake? WebhookID, string? WebhookToken, LogChannelType Type);
