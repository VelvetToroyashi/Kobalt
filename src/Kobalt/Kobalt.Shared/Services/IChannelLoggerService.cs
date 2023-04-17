using Kobalt.Shared.Types;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Shared.Services;

/// <summary>
/// Represents an abstraction for logging on Discord.
/// </summary>
public interface IChannelLoggerService
{
    /// <summary>
    /// Logs a message to all configured channels in a guild, if any.
    /// </summary>
    /// <param name="guildID">The ID of the guild to log to.</param>
    /// <param name="type">The type of logging being done.</param>
    /// <param name="content">The text content to log.</param>
    /// <param name="embeds">The embeds to log, if any.</param>
    /// <returns>A result indicating whether logging was successful.</returns>
    public ValueTask<Result> LogAsync(Snowflake guildID, LogChannelType type, Optional<string> content = default, Optional<IReadOnlyList<Embed>> embeds = default);
}
