using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Kobalt.Infractions.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Remora.Rest.Core;

namespace Kobalt.Bot.Data.Entities;

/// <summary>
/// Represents various settings related to phishing detection.
/// </summary>
public class GuildPhishingConfig
{
    public int Id { get; set; }
    public required Snowflake GuildID { get; set; }

    [NotNull]
    public Guild? Guild { get; set; }

    public bool ScanLinks { get; set; }
    public bool ScanUsers { get; set; }
    public InfractionType DetectionAction { get; set; }

}

public class GuildPhishingConfigEntityConfiguration : IEntityTypeConfiguration<GuildPhishingConfig>
{
    public void Configure(EntityTypeBuilder<GuildPhishingConfig> builder)
    {
        builder.ToTable("guild_phishing_configs");
    }
}
