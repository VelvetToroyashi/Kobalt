using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Remora.Rest.Core;

namespace Kobalt.Bot.Data.Entities.RoleMenus;

public class RoleMenuEntity
{
    public int Id { get; set; }
    
    public Snowflake GuildID { get; set; }
    public Snowflake ChannelID { get; set; }
    public Snowflake MessageID { get; set; }

    [NotNull]
    public KobaltGuild? Guild { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }
    
    public int MaxSelections { get; set; }
    
    public List<RoleMenuOptionEntity> Options { get; set; }
}

public class RoleMenuEntityConfiguration : IEntityTypeConfiguration<RoleMenuEntity>
{
    public void Configure(EntityTypeBuilder<RoleMenuEntity> builder)
    {
        builder.ToTable("role_menus");
    }
} 