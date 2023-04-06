using Kobalt.Infractions.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kobalt.Infractions.Data.Entities;

public class Infraction
{
    public int Id { get; set; }
    public int? ReferencedId { get; set; }
    
    public bool IsHidden { get; set; }
    public bool IsProcessable { get; set; }
    public required string Reason { get; set; }

    public required ulong UserID { get; set; }
    public required ulong GuildID { get; set; }
    public required ulong ModeratorID { get; set; }
    
    public required InfractionType Type { get; set; }
    
    public required DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    
    public DateTimeOffset? LastUpdated { get; set; }
    
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
