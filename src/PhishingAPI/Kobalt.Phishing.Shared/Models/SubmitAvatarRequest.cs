using Remora.Rest.Core;

namespace Kobalt.Phishing.Shared.Models;

public record SubmitAvatarRequest
(
    string Url,
    string Category,
    Snowflake AddedBy
);
