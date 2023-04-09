using Kobalt.Infractions.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kobalt.Infractions.Data.Entities;

/// <summary>
/// Represents an infraction.
/// </summary>
public class Infraction
{
    /// <summary>
    /// The ID of the infraction.
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// The ID of the referenced infraction, if applicable.
    /// </summary>
    public int? ReferencedId { get; set; }
    
    /// <summary>
    /// Whether this infraction is hidden.
    /// </summary>
    public bool IsHidden { get; set; }
    
    /// <summary>
    /// Whether this infraction is processable.
    /// </summary>
    // ?? What is this for?
    public bool IsProcessable { get; set; }

    /// <summary>
    /// The reason for this infraction.
    /// </summary>
    public required string Reason { get; set; }

    /// <summary>
    /// The ID of the target user.
    /// </summary>
    public required ulong UserID { get; set; }
    
    /// <summary>
    /// The ID of the guild this infraction was created in.
    /// </summary>
    public required ulong GuildID { get; set; }
    
    /// <summary>
    /// The ID of the moderator that created this infraction.
    /// </summary>
    public required ulong ModeratorID { get; set; }
    
    /// <summary>
    /// The type of this infraction.
    /// </summary>
    public required InfractionType Type { get; set; }
    
    /// <summary>
    /// The time this infraction was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; set; }
    
    /// <summary>
    /// When this infraction expires, if applicable.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }
    
    /// <summary>
    /// When the infraction was last updated, if ever.
    /// </summary>
    public DateTimeOffset? LastUpdated { get; set; }
    
    /// <summary>
    /// Foreign key to the referenced infraction, if applicable.
    /// </summary>
    public Infraction? Referenced { get; set; }
}

public class InfractionEntityConfig : IEntityTypeConfiguration<Infraction>
{
    public void Configure(EntityTypeBuilder<Infraction> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.IsHidden).IsRequired();
        builder.Property(x => x.Reason).IsRequired();

        builder.Property(x => x.LastUpdated).IsRowVersion();

        builder.HasOne(x => x.Referenced)
               .WithOne()
               .HasForeignKey<Infraction>(x => x.ReferencedId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
