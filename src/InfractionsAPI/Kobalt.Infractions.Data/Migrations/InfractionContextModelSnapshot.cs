﻿// <auto-generated />
using System;
using Kobalt.Infractions.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Kobalt.Infractions.Data.Migrations
{
    [DbContext(typeof(InfractionContext))]
    partial class InfractionContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("kobalt_infractions")
                .HasAnnotation("ProductVersion", "7.0.9")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Kobalt.Infractions.Data.Entities.Infraction", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<DateTimeOffset?>("ExpiresAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("expires_at");

                    b.Property<decimal>("GuildID")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("guild_id");

                    b.Property<bool>("IsHidden")
                        .HasColumnType("boolean")
                        .HasColumnName("is_hidden");

                    b.Property<bool>("IsProcessable")
                        .HasColumnType("boolean")
                        .HasColumnName("is_processable");

                    b.Property<DateTimeOffset?>("LastUpdated")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("last_updated");

                    b.Property<decimal>("ModeratorID")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("moderator_id");

                    b.Property<string>("Reason")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("reason");

                    b.Property<int?>("ReferencedId")
                        .HasColumnType("integer")
                        .HasColumnName("referenced_id");

                    b.Property<int>("Type")
                        .HasColumnType("integer")
                        .HasColumnName("type");

                    b.Property<decimal>("UserID")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("user_id");

                    b.HasKey("Id")
                        .HasName("pk_infractions");

                    b.HasIndex("ReferencedId")
                        .HasDatabaseName("ix_infractions_referenced_id");

                    b.ToTable("infractions", "kobalt_infractions");
                });

            modelBuilder.Entity("Kobalt.Infractions.Data.Entities.InfractionRule", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<TimeSpan?>("ActionDuration")
                        .HasColumnType("interval")
                        .HasColumnName("action_duration");

                    b.Property<int>("ActionType")
                        .HasColumnType("integer")
                        .HasColumnName("action_type");

                    b.Property<decimal>("GuildID")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("guild_id");

                    b.Property<TimeSpan?>("MatchTimeSpan")
                        .HasColumnType("interval")
                        .HasColumnName("match_time_span");

                    b.Property<int>("MatchType")
                        .HasColumnType("integer")
                        .HasColumnName("match_type");

                    b.Property<int>("MatchValue")
                        .HasColumnType("integer")
                        .HasColumnName("match_value");

                    b.Property<string>("RuleName")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("rule_name");

                    b.HasKey("Id")
                        .HasName("pk_infraction_rules");

                    b.ToTable("infraction_rules", "kobalt_infractions");
                });

            modelBuilder.Entity("Kobalt.Infractions.Data.Entities.Infraction", b =>
                {
                    b.HasOne("Kobalt.Infractions.Data.Entities.Infraction", "Referenced")
                        .WithMany()
                        .HasForeignKey("ReferencedId")
                        .HasConstraintName("fk_infractions_infractions_referenced_id");

                    b.Navigation("Referenced");
                });
#pragma warning restore 612, 618
        }
    }
}
