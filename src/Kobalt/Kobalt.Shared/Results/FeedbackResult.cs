using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;

namespace Kobalt.Shared.Results;

/// <summary>
/// Represents a message to be sent to the user.
/// </summary>
/// <param name="Message">String content to send.</param>
/// <param name="Embeds">Any embeds to send.</param>
public record FeedbackResult(Optional<string> Message = default, Optional<IEnumerable<IEmbed>> Embeds = default);
