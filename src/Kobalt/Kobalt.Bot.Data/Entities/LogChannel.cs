using System.Diagnostics.CodeAnalysis;
using Kobalt.Shared.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Remora.Rest.Core;

namespace Kobalt.Bot.Data.Entities;
public class LogChannel
{
    public Snowflake GuildID { get; set; }
    public Snowflake ChannelID { get; set; }

    public Snowflake? WebhookID { get; set; }
    public string? WebhookToken { get; set; }

    public LogChannelType Type { get; set; }
    
    [NotNull]
    public KobaltGuild? Guild { get; set; }
}

public class GuildLogChannelConfiguration : IEntityTypeConfiguration<LogChannel>
{
    public void Configure(EntityTypeBuilder<LogChannel> builder)
    {
        builder.HasKey(l => l.ChannelID);
        builder.ToTable("log_channels");
        builder.HasIndex(l => l.GuildID);
    }
}
