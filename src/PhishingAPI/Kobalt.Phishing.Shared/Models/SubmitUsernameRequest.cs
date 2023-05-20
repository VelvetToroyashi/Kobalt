namespace Kobalt.Phishing.Shared.Models;

/// <summary>
/// Represents a request to upload a username to the phishing database.
/// </summary>
/// <param name="UsernamePattern">The pattern to match usernames against.</param>
public record SubmitUsernameRequest (string UsernamePattern, UsernameParseType ParseType);

/// <summary>
/// Dictates how usernames should be parsed.
/// </summary>
public enum UsernameParseType
{
    /// <summary>
    /// The username should be parsed as a literal string.
    /// </summary>
    Literal,

    /// <summary>
    /// The username is interpreted as a regular expression.
    /// </summary>
    Regex
}
