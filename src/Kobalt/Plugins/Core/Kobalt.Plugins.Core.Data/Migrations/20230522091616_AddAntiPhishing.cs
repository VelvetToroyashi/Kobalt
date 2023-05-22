using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Kobalt.Plugins.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAntiPhishing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "guild_phishing_config",
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
                    table.PrimaryKey("pk_guild_phishing_config", x => x.id);
                    table.ForeignKey(
                        name: "fk_guild_phishing_config_guilds_guild_id",
                        column: x => x.guild_id,
                        principalSchema: "kobalt_core",
                        principalTable: "guilds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_guild_phishing_config_guild_id",
                schema: "kobalt_core",
                table: "guild_phishing_config",
                column: "guild_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "guild_phishing_config",
                schema: "kobalt_core");
        }
    }
}
