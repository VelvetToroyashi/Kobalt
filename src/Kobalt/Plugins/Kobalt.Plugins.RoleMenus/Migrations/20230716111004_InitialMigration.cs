using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Kobalt.Plugins.RoleMenus.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "kobalt_plugins_rolemenu");

            migrationBuilder.CreateTable(
                name: "role_menus",
                schema: "kobalt_plugins_rolemenu",
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
                name: "role_menu_options",
                schema: "kobalt_plugins_rolemenu",
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
                        principalSchema: "kobalt_plugins_rolemenu",
                        principalTable: "role_menus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_role_menu_options_role_menu_id",
                schema: "kobalt_plugins_rolemenu",
                table: "role_menu_options",
                column: "role_menu_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_menus_guild_id",
                schema: "kobalt_plugins_rolemenu",
                table: "role_menus",
                column: "guild_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "role_menu_options",
                schema: "kobalt_plugins_rolemenu");

            migrationBuilder.DropTable(
                name: "role_menus",
                schema: "kobalt_plugins_rolemenu");
        }
    }
}
