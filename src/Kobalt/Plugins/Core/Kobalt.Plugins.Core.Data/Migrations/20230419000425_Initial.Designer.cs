﻿// <auto-generated />
using Kobalt.Plugins.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Kobalt.Plugins.Core.Data.Migrations
{
    [DbContext(typeof(KobaltContext))]
    [Migration("20230419000425_Initial")]
    partial class Initial
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("kobalt_core")
                .HasAnnotation("ProductVersion", "7.0.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Kobalt.Plugins.Core.Data.Entities.Guild", b =>
                {
                    b.Property<ulong>("ID")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("id");

                    b.HasKey("ID")
                        .HasName("pk_guilds");

                    b.ToTable("guilds", "kobalt_core");
                });

            modelBuilder.Entity("Kobalt.Plugins.Core.Data.Entities.GuildUserJoiner", b =>
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
                        .HasName("pk_guild_user_joiner");

                    b.HasIndex("GuildId")
                        .HasDatabaseName("ix_guild_user_joiner_guild_id");

                    b.HasIndex("UserId")
                        .HasDatabaseName("ix_guild_user_joiner_user_id");

                    b.ToTable("guild_user_joiner", "kobalt_core");
                });

            modelBuilder.Entity("Kobalt.Plugins.Core.Data.Entities.LogChannel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

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

                    b.HasKey("Id")
                        .HasName("pk_log_channels");

                    b.HasIndex("GuildID")
                        .HasDatabaseName("ix_log_channels_guild_id");

                    b.ToTable("log_channels", "kobalt_core");
                });

            modelBuilder.Entity("Kobalt.Plugins.Core.Data.Entities.User", b =>
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

            modelBuilder.Entity("Kobalt.Plugins.Core.Data.Entities.GuildUserJoiner", b =>
                {
                    b.HasOne("Kobalt.Plugins.Core.Data.Entities.Guild", "Guild")
                        .WithMany("Users")
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_guild_user_joiner_guilds_guild_id");

                    b.HasOne("Kobalt.Plugins.Core.Data.Entities.User", "User")
                        .WithMany("Guilds")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_guild_user_joiner_users_user_id");

                    b.Navigation("Guild");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Kobalt.Plugins.Core.Data.Entities.LogChannel", b =>
                {
                    b.HasOne("Kobalt.Plugins.Core.Data.Entities.Guild", null)
                        .WithMany("LogChannels")
                        .HasForeignKey("GuildID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_log_channels_guilds_guild_id");
                });

            modelBuilder.Entity("Kobalt.Plugins.Core.Data.Entities.Guild", b =>
                {
                    b.Navigation("LogChannels");

                    b.Navigation("Users");
                });

            modelBuilder.Entity("Kobalt.Plugins.Core.Data.Entities.User", b =>
                {
                    b.Navigation("Guilds");
                });
#pragma warning restore 612, 618
        }
    }
}