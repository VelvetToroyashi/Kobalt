﻿using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Remora.Rest.Core;
using NotNullAttribute = System.Diagnostics.CodeAnalysis.NotNullAttribute;

namespace Kobalt.Plugins.Core.Data.Entities;

[PublicAPI]
public class Guild
{
    /// <summary>
    /// The ID of the guild.
    /// </summary>
    public Snowflake ID { get; set; }

    [NotNull]
    public GuildAutoModConfig? AutoModConfig { get; set; }

    [NotNull]
    public GuildAntiRaidConfig? AntiRaidConfig { get; set; }

    [NotNull]
    public GuildPhishingConfig? PhishingConfig { get; set; }

    /// <summary>
    /// Users on the guild.
    /// </summary>
    public List<GuildUserJoiner> Users { get; set; } = new();

    /// <summary>
    /// Logging channels on the guild.
    /// </summary>
    public List<LogChannel> LogChannels { get; set; } = new();
    // TODO: Configurations
}

public class GuildEntityConfiguration : IEntityTypeConfiguration<Guild>
{
    public void Configure(EntityTypeBuilder<Guild> builder)
    {
        builder.ToTable("Guilds", KobaltContext.Schema);
    }
}