using Remora.Results;

namespace Kobalt.Infrastructure.Types.Results;

/// <summary>
/// Represents an error where an action is forbidden due to being blacklisted for any reason.
/// </summary>
/// <param name="Message">The message.</param>
public record BlacklistedError(string Message) : ResultError(Message);
