using Remora.Rest.Core;

namespace Kobalt.Plugins.Core.Data.Entities;

public class Guild
{
    public Snowflake ID { get; set; }
    
    public List<GuildUserJoiner> Users { get; set; } = new();
    
    public List<LogChannel> LogChannels { get; set; } = new();
    // TODO: Configurations
}