namespace Kobalt.Phishing.Shared.Models;

/// <summary>
/// Represents a result for detecting phishing from a user
/// </summary>
/// <param name="Score"></param>
/// <param name="Global"></param>
/// <param name="DetectionReason">The reason the match was detected.</param>
public record UserPhishingDetectionResult
(
    int? Score,
    bool Match,
    bool Global,
    string? DetectionReason
);
