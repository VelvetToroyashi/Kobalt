using JetBrains.Annotations;
using Remora.Rest.Core;

namespace Kobalt.Plugins.Core.Data.Entities;

[PublicAPI]
public class Guild
{
    /// <summary>
    /// The ID of the guild.
    /// </summary>
    public Snowflake ID { get; set; }

    [System.Diagnostics.CodeAnalysis.NotNull]
    public GuildAutoModConfig? AutoModConfig { get; set; }

    [System.Diagnostics.CodeAnalysis.NotNull]
    public GuildAntiRaidConfig? AntiRaidConfig { get; set; }

    [System.Diagnostics.CodeAnalysis.NotNull]
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
