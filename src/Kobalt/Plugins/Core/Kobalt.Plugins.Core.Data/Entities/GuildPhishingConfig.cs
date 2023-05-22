using System.Diagnostics.CodeAnalysis;
using Kobalt.Infractions.Shared;

namespace Kobalt.Plugins.Core.Data.Entities;

/// <summary>
/// Represents various settings related to phishing detection.
/// </summary>
public class GuildPhishingConfig
{
    public int Id { get; set; }
    public ulong GuildId { get; set; }

    public bool ScanLinks { get; set; }
    public bool ScanUsers { get; set; }
    public InfractionType DetectionAction { get; set; }

    [NotNull]
    public Guild? Guild { get; set; }
}
