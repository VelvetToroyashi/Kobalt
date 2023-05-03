using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Remora.Rest.Core;

namespace Kobalt.Plugins.Core.Data.Entities;

public class GuildAutoModConfig
{
    /// <summary>
    /// The ID of this configuration.
    /// </summary>
    public int Id { get; set; }

    public Snowflake GuildId { get; set; }

    /// <summary>
    /// The threshold for Kobalt to transition a channel to push-to-talk and back.
    /// </summary>
    public int? PushToTalkThreshold { get; set; }




    [NotNull]
    public Guild? Guild { get; set; }
}

public class GuildAutoModConfigConfiguration : IEntityTypeConfiguration<GuildAutoModConfig>
{
    public void Configure(EntityTypeBuilder<GuildAutoModConfig> builder)
    {
        builder.HasOne(x => x.Guild).WithOne(x => x.AutoModConfig).HasForeignKey<GuildAutoModConfig>(x => x.GuildId);
    }
}
