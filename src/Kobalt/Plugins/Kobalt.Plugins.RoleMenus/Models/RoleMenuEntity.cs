using System.Diagnostics.CodeAnalysis;
using Kobalt.Plugins.Core.Data.Entities;
using Remora.Rest.Core;

namespace Kobalt.Plugins.RoleMenus.Models;

public class RoleMenuEntity
{
    public int Id { get; set; }
    
    public Snowflake GuildID { get; set; }
    public Snowflake ChannelID { get; set; }
    public Snowflake MessageID { get; set; }

    [NotNull]
    public Guild? Guild { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }
    
    public int MaxSelections { get; set; }
    
    public List<RoleMenuOptionEntity> Options { get; set; }
}