using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Remora.Rest.Core;

namespace Kobalt.Plugins.Core.Data.Entities;

public class GuildUserJoiner
{
    public int Id { get; set; }
    public Snowflake UserId { get; set; }
    public Snowflake GuildId { get; set; }

    [NotNull]
    public User? User { get; set; }

    [NotNull]
    public Guild? Guild { get; set; }
}

public class GuildUserJoinerConfiguration : IEntityTypeConfiguration<GuildUserJoiner>
{
    public void Configure(EntityTypeBuilder<GuildUserJoiner> builder)
    {
        builder.ToTable("GuildUserJoiners", KobaltContext.Schema);
        
        builder.HasOne(x => x.User).WithMany(x => x.Guilds).HasForeignKey(x => x.UserId);
        builder.HasOne(x => x.Guild).WithMany(x => x.Users).HasForeignKey(x => x.GuildId);
    }
}
