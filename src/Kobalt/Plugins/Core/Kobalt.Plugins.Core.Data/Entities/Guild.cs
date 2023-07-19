using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Remora.Rest.Core;

namespace Kobalt.Plugins.Core.Data.Entities;

[Table("guilds", Schema = KobaltContext.Schema)]
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