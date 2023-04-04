namespace Kobalt.Artists.Data.Entities;

public class Artist
{
    public ulong       DiscordId        { get; set; }
    public ulong?      VerifiedBy       { get; set; }
    public ArtistFlags Flags            { get; set; }
    public DateTime?   VerifiedAt       { get; set; }
    public string?     TwitterHandle    { get; set; }
    public string?     FurAffinityName  { get; set; }
}
