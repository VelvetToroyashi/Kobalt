namespace Kobalt.Artists.Data.Entities;

/// <summary>
/// Flags for an artist, such whether they've been verified or are known as an art scammer.
/// </summary>
[Flags]
public enum ArtistFlags
{
    PendingVerification,
    AutomaticallyVerified,
    ManuallyVerified,
    PreviouslyVerified,
    MarkedAsSuspicious,
    KnownFraudulentArtist,
}
