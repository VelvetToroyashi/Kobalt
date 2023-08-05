using Kobalt.Shared.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Remora.Rest.Core;

namespace Kobalt.Bot.Data.Entities;
public class LogChannel
{
    public int Id { get; set; }

    public Snowflake GuildID { get; set; }
    public Snowflake ChannelID { get; set; }

    public Snowflake? WebhookID { get; set; }
    public string? WebhookToken { get; set; }

    public LogChannelType Type { get; set; }
}

public class GuildLogChannelConfiguration : IEntityTypeConfiguration<LogChannel>
{
    public void Configure(EntityTypeBuilder<LogChannel> builder)
    {
        builder.ToTable("log_channels");
        builder.HasIndex(l => l.GuildID);
    }
}
