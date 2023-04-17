using Remora.Rest.Core;

namespace Kobalt.Data.DTOs;

public record LogChannelDTO(Snowflake ChannelID, Snowflake? WebhookID, string? WebhookToken);
