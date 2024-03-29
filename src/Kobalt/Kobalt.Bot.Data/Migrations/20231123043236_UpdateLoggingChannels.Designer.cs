﻿// <auto-generated />
using System;
using Kobalt.Bot.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Kobalt.Bot.Data.Migrations
{
    [DbContext(typeof(KobaltContext))]
    [Migration("20231123043236_UpdateLoggingChannels")]
    partial class UpdateLoggingChannels
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("kobalt_core")
                .HasAnnotation("ProductVersion", "7.0.9")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Kobalt.Bot.Data.Entities.GuildAntiRaidConfig", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int?>("AccountFlagsBypass")
                        .HasColumnType("integer")
                        .HasColumnName("account_flags_bypass");

                    b.Property<TimeSpan>("AntiRaidCooldownPeriod")
                        .HasColumnType("interval")
                        .HasColumnName("anti_raid_cooldown_period");

                    b.Property<int>("BaseJoinScore")
                        .HasColumnType("integer")
                        .HasColumnName("base_join_score");

                    b.Property<ulong>("GuildID")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("guild_id");

                    b.Property<bool>("IsEnabled")
                        .HasColumnType("boolean")
                        .HasColumnName("is_enabled");

                    b.Property<int>("JoinVelocityScore")
                        .HasColumnType("integer")
                        .HasColumnName("join_velocity_score");

                    b.Property<TimeSpan>("LastJoinBufferPeriod")
                        .HasColumnType("interval")
                        .HasColumnName("last_join_buffer_period");

                    b.Property<TimeSpan>("MinimumAccountAge")
                        .HasColumnType("interval")
                        .HasColumnName("minimum_account_age");

                    b.Property<int>("MinimumAgeScore")
                        .HasColumnType("integer")
                        .HasColumnName("minimum_age_score");

                    b.Property<TimeSpan?>("MiniumAccountAgeBypass")
                        .HasColumnType("interval")
                        .HasColumnName("minium_account_age_bypass");

                    b.Property<int>("NoAvatarScore")
                        .HasColumnType("integer")
                        .HasColumnName("no_avatar_score");

                    b.Property<int>("SuspiciousInviteScore")
                        .HasColumnType("integer")
                        .HasColumnName("suspicious_invite_score");

                    b.Property<int>("ThreatScoreThreshold")
                        .HasColumnType("integer")
                        .HasColumnName("threat_score_threshold");

                    b.HasKey("Id")
                        .HasName("pk_guild_anti_raid_configs");

                    b.HasIndex("GuildID")
                        .IsUnique()
                        .HasDatabaseName("ix_guild_anti_raid_configs_guild_id");

                    b.ToTable("guild_anti-raid_configs", "kobalt_core");
                });

            modelBuilder.Entity("Kobalt.Bot.Data.Entities.GuildAutoModConfig", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<ulong>("GuildID")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("guild_id");

                    b.Property<int?>("PushToTalkThreshold")
                        .HasColumnType("integer")
                        .HasColumnName("push_to_talk_threshold");

                    b.HasKey("Id")
                        .HasName("pk_guild_automod_configs");

                    b.HasIndex("GuildID")
                        .IsUnique()
                        .HasDatabaseName("ix_guild_automod_configs_guild_id");

                    b.ToTable("guild_automod_configs", "kobalt_core");
                });

            modelBuilder.Entity("Kobalt.Bot.Data.Entities.GuildPhishingConfig", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("DetectionAction")
                        .HasColumnType("integer")
                        .HasColumnName("detection_action");

                    b.Property<ulong>("GuildID")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("guild_id");

                    b.Property<bool>("ScanLinks")
                        .HasColumnType("boolean")
                        .HasColumnName("scan_links");

                    b.Property<bool>("ScanUsers")
                        .HasColumnType("boolean")
                        .HasColumnName("scan_users");

                    b.HasKey("Id")
                        .HasName("pk_guild_phishing_configs");

                    b.HasIndex("GuildID")
                        .IsUnique()
                        .HasDatabaseName("ix_guild_phishing_configs_guild_id");

                    b.ToTable("guild_phishing_configs", "kobalt_core");
                });

            modelBuilder.Entity("Kobalt.Bot.Data.Entities.GuildUserJoiner", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<ulong>("GuildId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("guild_id");

                    b.Property<ulong>("UserId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("user_id");

                    b.HasKey("Id")
                        .HasName("pk_guild_user_joiners");

                    b.HasIndex("GuildId")
                        .HasDatabaseName("ix_guild_user_joiners_guild_id");

                    b.HasIndex("UserId")
                        .HasDatabaseName("ix_guild_user_joiners_user_id");

                    b.ToTable("guild_user_joiners", "kobalt_core");
                });

            modelBuilder.Entity("Kobalt.Bot.Data.Entities.KobaltGuild", b =>
                {
                    b.Property<ulong>("ID")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("id");

                    b.HasKey("ID")
                        .HasName("pk_guilds");

                    b.ToTable("guilds", "kobalt_core");
                });

            modelBuilder.Entity("Kobalt.Bot.Data.Entities.LogChannel", b =>
                {
                    b.Property<ulong>("ChannelID")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("channel_id");

                    b.Property<ulong>("GuildID")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("guild_id");

                    b.Property<decimal>("Type")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("type");

                    b.Property<ulong?>("WebhookID")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("webhook_id");

                    b.Property<string>("WebhookToken")
                        .HasColumnType("text")
                        .HasColumnName("webhook_token");

                    b.HasKey("ChannelID")
                        .HasName("pk_log_channels");

                    b.HasIndex("GuildID")
                        .HasDatabaseName("ix_log_channels_guild_id");

                    b.ToTable("log_channels", "kobalt_core");
                });

            modelBuilder.Entity("Kobalt.Bot.Data.Entities.RoleMenus.RoleMenuEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<ulong>("ChannelID")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("channel_id");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("description");

                    b.Property<ulong>("GuildID")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("guild_id");

                    b.Property<int>("MaxSelections")
                        .HasColumnType("integer")
                        .HasColumnName("max_selections");

                    b.Property<ulong>("MessageID")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("message_id");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.HasKey("Id")
                        .HasName("pk_role_menus");

                    b.HasIndex("GuildID")
                        .HasDatabaseName("ix_role_menus_guild_id");

                    b.ToTable("role_menus", "kobalt_core");
                });

            modelBuilder.Entity("Kobalt.Bot.Data.Entities.RoleMenus.RoleMenuOptionEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("description");

                    b.Property<string>("MutuallyExclusiveRoles")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("mutually_exclusive_roles");

                    b.Property<string>("MutuallyInclusiveRoles")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("mutually_inclusive_roles");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.Property<ulong>("RoleID")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("role_id");

                    b.Property<int>("RoleMenuId")
                        .HasColumnType("integer")
                        .HasColumnName("role_menu_id");

                    b.HasKey("Id")
                        .HasName("pk_role_menu_options");

                    b.HasIndex("RoleMenuId")
                        .HasDatabaseName("ix_role_menu_options_role_menu_id");

                    b.ToTable("role_menu_options", "kobalt_core");
                });

            modelBuilder.Entity("Kobalt.Bot.Data.Entities.User", b =>
                {
                    b.Property<ulong>("ID")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("id");

                    b.Property<bool>("DisplayTimezone")
                        .HasColumnType("boolean")
                        .HasColumnName("display_timezone");

                    b.Property<string>("Timezone")
                        .HasColumnType("text")
                        .HasColumnName("timezone");

                    b.HasKey("ID")
                        .HasName("pk_users");

                    b.ToTable("users", "kobalt_core");
                });

            modelBuilder.Entity("Kobalt.Bot.Data.Entities.GuildAntiRaidConfig", b =>
                {
                    b.HasOne("Kobalt.Bot.Data.Entities.KobaltGuild", "Guild")
                        .WithOne("AntiRaidConfig")
                        .HasForeignKey("Kobalt.Bot.Data.Entities.GuildAntiRaidConfig", "GuildID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_guild_anti_raid_configs_guilds_guild_id");

                    b.Navigation("Guild");
                });

            modelBuilder.Entity("Kobalt.Bot.Data.Entities.GuildAutoModConfig", b =>
                {
                    b.HasOne("Kobalt.Bot.Data.Entities.KobaltGuild", "Guild")
                        .WithOne("AutoModConfig")
                        .HasForeignKey("Kobalt.Bot.Data.Entities.GuildAutoModConfig", "GuildID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_guild_automod_configs_guilds_guild_id");

                    b.Navigation("Guild");
                });

            modelBuilder.Entity("Kobalt.Bot.Data.Entities.GuildPhishingConfig", b =>
                {
                    b.HasOne("Kobalt.Bot.Data.Entities.KobaltGuild", "Guild")
                        .WithOne("PhishingConfig")
                        .HasForeignKey("Kobalt.Bot.Data.Entities.GuildPhishingConfig", "GuildID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_guild_phishing_configs_guilds_guild_id");

                    b.Navigation("Guild");
                });

            modelBuilder.Entity("Kobalt.Bot.Data.Entities.GuildUserJoiner", b =>
                {
                    b.HasOne("Kobalt.Bot.Data.Entities.KobaltGuild", "Guild")
                        .WithMany("Users")
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_guild_user_joiners_guilds_guild_id");

                    b.HasOne("Kobalt.Bot.Data.Entities.User", "User")
                        .WithMany("Guilds")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_guild_user_joiners_users_user_id");

                    b.Navigation("Guild");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Kobalt.Bot.Data.Entities.LogChannel", b =>
                {
                    b.HasOne("Kobalt.Bot.Data.Entities.KobaltGuild", "Guild")
                        .WithMany("LogChannels")
                        .HasForeignKey("GuildID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_log_channels_guilds_guild_id");

                    b.Navigation("Guild");
                });

            modelBuilder.Entity("Kobalt.Bot.Data.Entities.RoleMenus.RoleMenuEntity", b =>
                {
                    b.HasOne("Kobalt.Bot.Data.Entities.KobaltGuild", "Guild")
                        .WithMany()
                        .HasForeignKey("GuildID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_role_menus_guilds_guild_id");

                    b.Navigation("Guild");
                });

            modelBuilder.Entity("Kobalt.Bot.Data.Entities.RoleMenus.RoleMenuOptionEntity", b =>
                {
                    b.HasOne("Kobalt.Bot.Data.Entities.RoleMenus.RoleMenuEntity", "RoleMenu")
                        .WithMany("Options")
                        .HasForeignKey("RoleMenuId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_role_menu_options_role_menus_role_menu_id");

                    b.Navigation("RoleMenu");
                });

            modelBuilder.Entity("Kobalt.Bot.Data.Entities.KobaltGuild", b =>
                {
                    b.Navigation("AntiRaidConfig")
                        .IsRequired();

                    b.Navigation("AutoModConfig")
                        .IsRequired();

                    b.Navigation("LogChannels");

                    b.Navigation("PhishingConfig")
                        .IsRequired();

                    b.Navigation("Users");
                });

            modelBuilder.Entity("Kobalt.Bot.Data.Entities.RoleMenus.RoleMenuEntity", b =>
                {
                    b.Navigation("Options");
                });

            modelBuilder.Entity("Kobalt.Bot.Data.Entities.User", b =>
                {
                    b.Navigation("Guilds");
                });
#pragma warning restore 612, 618
        }
    }
}
