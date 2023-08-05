using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Kobalt.Bot.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "kobalt_core");

            migrationBuilder.CreateTable(
                name: "guilds",
                schema: "kobalt_core",
                columns: table => new
                {
                    id = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guilds", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "kobalt_core",
                columns: table => new
                {
                    id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    timezone = table.Column<string>(type: "text", nullable: true),
                    display_timezone = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "guild_anti-raid_configs",
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
                    table.PrimaryKey("pk_guild_anti_raid_configs", x => x.id);
                    table.ForeignKey(
                        name: "fk_guild_anti_raid_configs_guilds_guild_id",
                        column: x => x.guild_id,
                        principalSchema: "kobalt_core",
                        principalTable: "guilds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "guild_automod_configs",
                schema: "kobalt_core",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guild_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    push_to_talk_threshold = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guild_automod_configs", x => x.id);
                    table.ForeignKey(
                        name: "fk_guild_automod_configs_guilds_guild_id",
                        column: x => x.guild_id,
                        principalSchema: "kobalt_core",
                        principalTable: "guilds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "guild_phishing_configs",
                schema: "kobalt_core",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guild_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    scan_links = table.Column<bool>(type: "boolean", nullable: false),
                    scan_users = table.Column<bool>(type: "boolean", nullable: false),
                    detection_action = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guild_phishing_configs", x => x.id);
                    table.ForeignKey(
                        name: "fk_guild_phishing_configs_guilds_guild_id",
                        column: x => x.guild_id,
                        principalSchema: "kobalt_core",
                        principalTable: "guilds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "log_channels",
                schema: "kobalt_core",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guild_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    channel_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    webhook_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    webhook_token = table.Column<string>(type: "text", nullable: true),
                    type = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_log_channels", x => x.id);
                    table.ForeignKey(
                        name: "fk_log_channels_guilds_guild_id",
                        column: x => x.guild_id,
                        principalSchema: "kobalt_core",
                        principalTable: "guilds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role_menus",
                schema: "kobalt_core",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guild_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    channel_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    message_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    max_selections = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_menus", x => x.id);
                    table.ForeignKey(
                        name: "fk_role_menus_guilds_guild_id",
                        column: x => x.guild_id,
                        principalSchema: "kobalt_core",
                        principalTable: "guilds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "guild_user_joiners",
                schema: "kobalt_core",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    guild_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guild_user_joiners", x => x.id);
                    table.ForeignKey(
                        name: "fk_guild_user_joiners_guilds_guild_id",
                        column: x => x.guild_id,
                        principalSchema: "kobalt_core",
                        principalTable: "guilds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_guild_user_joiners_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "kobalt_core",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role_menu_options",
                schema: "kobalt_core",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_menu_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    role_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    mutually_inclusive_roles = table.Column<string>(type: "text", nullable: false),
                    mutually_exclusive_roles = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_menu_options", x => x.id);
                    table.ForeignKey(
                        name: "fk_role_menu_options_role_menus_role_menu_id",
                        column: x => x.role_menu_id,
                        principalSchema: "kobalt_core",
                        principalTable: "role_menus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_guild_anti_raid_configs_guild_id",
                schema: "kobalt_core",
                table: "guild_anti-raid_configs",
                column: "guild_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_guild_automod_configs_guild_id",
                schema: "kobalt_core",
                table: "guild_automod_configs",
                column: "guild_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_guild_phishing_configs_guild_id",
                schema: "kobalt_core",
                table: "guild_phishing_configs",
                column: "guild_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_guild_user_joiners_guild_id",
                schema: "kobalt_core",
                table: "guild_user_joiners",
                column: "guild_id");

            migrationBuilder.CreateIndex(
                name: "ix_guild_user_joiners_user_id",
                schema: "kobalt_core",
                table: "guild_user_joiners",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_log_channels_guild_id",
                schema: "kobalt_core",
                table: "log_channels",
                column: "guild_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_menu_options_role_menu_id",
                schema: "kobalt_core",
                table: "role_menu_options",
                column: "role_menu_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_menus_guild_id",
                schema: "kobalt_core",
                table: "role_menus",
                column: "guild_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "guild_anti-raid_configs",
                schema: "kobalt_core");

            migrationBuilder.DropTable(
                name: "guild_automod_configs",
                schema: "kobalt_core");

            migrationBuilder.DropTable(
                name: "guild_phishing_configs",
                schema: "kobalt_core");

            migrationBuilder.DropTable(
                name: "guild_user_joiners",
                schema: "kobalt_core");

            migrationBuilder.DropTable(
                name: "log_channels",
                schema: "kobalt_core");

            migrationBuilder.DropTable(
                name: "role_menu_options",
                schema: "kobalt_core");

            migrationBuilder.DropTable(
                name: "users",
                schema: "kobalt_core");

            migrationBuilder.DropTable(
                name: "role_menus",
                schema: "kobalt_core");

            migrationBuilder.DropTable(
                name: "guilds",
                schema: "kobalt_core");
        }
    }
}
