using Remora.Rest.Core;

namespace Kobalt.Shared.Models.Phishing;

public record SubmitAvatarRequest
(
    string Url,
    string Category,
    Snowflake AddedBy
);
