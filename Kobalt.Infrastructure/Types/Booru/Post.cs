using System.Text.Json.Serialization;

namespace Kobalt.Infrastructure.Types.Booru;

public record PostHolder(IReadOnlyList<Post> Posts);

public record Post
(
        long           Id,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        File           File,
        Preview        Preview,
        Sample         Sample,
        Score          Score,
        Tags           Tags,
        string[]       LockedTags,
        long           ChangeSeq,
        Flags          Flags,
        Rating         Rating,
        long           FavCount,
        Uri[]          Sources,
        long[]         Pools,
        Relationships  Relationships,
        long?           ApproverId,
        long           UploaderId,
        string         Description,
        long           CommentCount,
        bool           IsFavorited,
        bool           HasNotes,
        double?        Duration
);

public record File
(
        long Width,
        long Height,
        Ext Ext,
        long Size,
        string Md5,
        Uri Url
);

public record Flags
(
        bool Pending,
        bool Flagged,
        bool NoteLocked,
        bool StatusLocked,
        bool RatingLocked,
        bool CommentDisabled,
        bool Deleted
);

public record Preview
(
        long Width,
        long Height,
        Uri  Url
);

public record Relationships
(
        long? ParentId,
        bool  HasChildren,
        bool  HasActiveChildren,
        long[] Children
);

public record Sample
(
        bool Has,
        long Height,
        long Width,
        Uri  Url,
        Alternates Alternates
);

public record Alternates
(
    [property: JsonPropertyName("720p")]    
    Variant In720P,
    [property: JsonPropertyName("480p")]
    Variant In480P,
    [property: JsonPropertyName("original")]
    Variant Original
);

public record Variant(string Type, long Height, long Width, Uri[]Urls);

public record Score(long Up, long Down, long Total);

public record Tags
(
        string[] General,
        string[] Species,
        string[] Character,
        string[] Copyright,
        string[] Artist,
        string[] Invalid,
        string[] Lore,
        string[] Meta
);

public enum Ext { Jpg, Png, Webm, Gif, Swf };

public enum Rating
{
    /// <summary>
    /// The post is marked as explicit.
    /// </summary>
    E,
    
    /// <summary>
    /// The post is marked as questionable.
    /// </summary>
    Q,
    
    /// <summary>
    /// The post is marked as safe.
    /// </summary>
    S
};

