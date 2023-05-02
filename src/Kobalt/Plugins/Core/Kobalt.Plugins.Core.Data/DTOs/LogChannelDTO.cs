using Remora.Rest.Core;

namespace Kobalt.Plugins.Core.Data.DTOs;

public record LogChannelDTO(Snowflake ChannelID, Snowflake? WebhookID, string? WebhookToken);
