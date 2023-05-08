using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Kobalt.Plugins.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAntiRaid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "guild_anti_raid_config",
                schema: "kobalt_core",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guild_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    minium_account_age_bypass = table.Column<TimeSpan>(type: "interval", nullable: true),
                    account_flags_bypass = table.Column<int>(type: "integer", nullable: true),
                    base_join_score = table.Column<int>(type: "integer", nullable: false),
                    join_velocity_score = table.Column<int>(type: "integer", nullable: false),
                    minimum_age_score = table.Column<int>(type: "integer", nullable: false),
                    no_avatar_score = table.Column<int>(type: "integer", nullable: false),
                    suspicious_invite_score = table.Column<int>(type: "integer", nullable: false),
                    threat_score_threshold = table.Column<int>(type: "integer", nullable: false),
                    anti_raid_cooldown_period = table.Column<TimeSpan>(type: "interval", nullable: false),
                    last_join_buffer_period = table.Column<TimeSpan>(type: "interval", nullable: false),
                    minimum_account_age = table.Column<TimeSpan>(type: "interval", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guild_anti_raid_config", x => x.id);
                    table.ForeignKey(
                        name: "fk_guild_anti_raid_config_guilds_guild_id",
                        column: x => x.guild_id,
                        principalSchema: "kobalt_core",
                        principalTable: "guilds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_guild_anti_raid_config_guild_id",
                schema: "kobalt_core",
                table: "guild_anti_raid_config",
                column: "guild_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "guild_anti_raid_config",
                schema: "kobalt_core");
        }
    }
}
