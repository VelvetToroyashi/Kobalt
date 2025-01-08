namespace Kobalt.Shared.Models.Phishing;

/// <summary>
/// Represents a result for detecting phishing from a user
/// </summary>
/// <param name="Score">How closely a result matches for a given user's avatar. This is ignored for usernames.</param>
/// <param name="Global">Whether the user was detected by global phishing data.</param>
/// <param name="DetectionReason">The reason the match was detected.</param>
public record UserPhishingDetectionResult
(
    int? Score,
    bool Match,
    bool Global,
    string? DetectionReason
);
