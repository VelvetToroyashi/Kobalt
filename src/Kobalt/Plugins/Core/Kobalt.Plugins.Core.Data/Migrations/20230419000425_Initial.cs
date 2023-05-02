using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Kobalt.Plugins.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
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
                name: "guild_user_joiner",
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
                    table.PrimaryKey("pk_guild_user_joiner", x => x.id);
                    table.ForeignKey(
                        name: "fk_guild_user_joiner_guilds_guild_id",
                        column: x => x.guild_id,
                        principalSchema: "kobalt_core",
                        principalTable: "guilds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_guild_user_joiner_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "kobalt_core",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_guild_user_joiner_guild_id",
                schema: "kobalt_core",
                table: "guild_user_joiner",
                column: "guild_id");

            migrationBuilder.CreateIndex(
                name: "ix_guild_user_joiner_user_id",
                schema: "kobalt_core",
                table: "guild_user_joiner",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_log_channels_guild_id",
                schema: "kobalt_core",
                table: "log_channels",
                column: "guild_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "guild_user_joiner",
                schema: "kobalt_core");

            migrationBuilder.DropTable(
                name: "log_channels",
                schema: "kobalt_core");

            migrationBuilder.DropTable(
                name: "users",
                schema: "kobalt_core");

            migrationBuilder.DropTable(
                name: "guilds",
                schema: "kobalt_core");
        }
    }
}
