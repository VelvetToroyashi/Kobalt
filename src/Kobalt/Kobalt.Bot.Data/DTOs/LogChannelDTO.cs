using Remora.Rest.Core;

namespace Kobalt.Bot.Data.DTOs;

public record LogChannelDTO(Snowflake ChannelID, Snowflake? WebhookID, string? WebhookToken);
