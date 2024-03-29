﻿using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;

namespace Kobalt.Bot.Data.Entities;

/// <summary>
/// Represents various settings related to anti-raid.
/// </summary>
public class GuildAntiRaidConfig
{
    public int Id { get; set; }
    public required Snowflake GuildID { get; set; }

    [NotNull]
    public KobaltGuild? Guild { get; set; }

    /// <summary>
    /// Whether or not anti-raid is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// The minimum account age required to bypass anti-raid.
    /// </summary>
    public TimeSpan? MiniumAccountAgeBypass { get; set; }

    /// <summary>
    /// Account flags (badges) that are allowed to bypass anti-raid.
    /// </summary>
    public UserFlags? AccountFlagsBypass { get; set; }

    /// <summary>
    /// The base score applied to all joins.
    /// </summary>
    public required int BaseJoinScore { get; set; }

    /// <summary>
    /// The score given to a user for joining the server shortly after another user.
    /// </summary>
    public required int JoinVelocityScore { get; set; }

    /// <summary>
    /// The score given to a user for their account age being too new.
    /// </summary>
    public required int MinimumAgeScore { get; set; }

    /// <summary>
    /// The score given to a user for not having an avatar.
    /// </summary>
    public required int NoAvatarScore { get; set; }

    /// <summary>
    /// The score given to a user for joining the server from a suspicious invite
    /// </summary>
    public required int SuspiciousInviteScore { get; set; }

    /// <summary>
    /// The threshold score to consider all users to be a threat.
    /// </summary>
    public required int ThreatScoreThreshold { get; set; }

    /// <summary>
    /// The cooldown period from the last join for a raid to no longer be considered active, and to clear the raid score.
    /// </summary>
    public required TimeSpan AntiRaidCooldownPeriod { get; set; }

    /// <summary>
    /// The period of time between joins that should be considered suspicious.
    /// </summary>
    public required TimeSpan LastJoinBufferPeriod { get; set; }

    /// <summary>
    /// The minimum age of an account that should be considered suspicious.
    /// </summary>
    public required TimeSpan MinimumAccountAge { get; set; }
    
    /// <summary>
    /// Creates a "default" configuration instance with the assigned guild ID.
    /// </summary>
    /// <param name="guildID">The ID of the guild to assign this configuration to.</param>
    /// <returns>The created configuration instance.</returns>
    public static GuildAntiRaidConfig Default(Snowflake guildID) => new()
    {
        GuildID = guildID,
        BaseJoinScore = 0,
        AccountFlagsBypass = null,
        JoinVelocityScore = 1,
        AntiRaidCooldownPeriod = TimeSpan.FromSeconds(20),
        MinimumAccountAge = TimeSpan.Zero,
        LastJoinBufferPeriod = TimeSpan.FromSeconds(5),
        MinimumAgeScore = 0,
        NoAvatarScore = 1,
        SuspiciousInviteScore = 1,
        ThreatScoreThreshold = 10
    };
}

public class GuildAntiRaidConfigEntityConfig : IEntityTypeConfiguration<GuildAntiRaidConfig>
{
    public void Configure(EntityTypeBuilder<GuildAntiRaidConfig> builder)
    {
        builder.ToTable("guild_anti-raid_configs");
    }
} 