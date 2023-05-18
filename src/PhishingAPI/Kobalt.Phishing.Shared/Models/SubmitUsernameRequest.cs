using System.Text.Json.Serialization;

namespace Kobalt.Phishing.Shared.Models;

/// <summary>
/// Represents a request to upload a username to the phishing database.
/// </summary>
public class SubmitUsernameRequest
{
    /// <summary>
    /// The pattern to match usernames against.
    /// </summary>
    [JsonPropertyName("username_pattern")]
    public string UsernamePattern { get; set; }

    /// <summary>
    /// Represents how the username should be parsed.
    /// </summary>
    [JsonPropertyName("parse_type")]
    public UsernameParseType ParseType { get; set; }
}

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
