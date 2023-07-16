using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Kobalt.Infractions.Shared;
using Remora.Rest.Core;

namespace Kobalt.Plugins.Core.Data.Entities;

/// <summary>
/// Represents various settings related to phishing detection.
/// </summary>
[Table("guild_phishing_configs", Schema = KobaltContext.Schema)]
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
