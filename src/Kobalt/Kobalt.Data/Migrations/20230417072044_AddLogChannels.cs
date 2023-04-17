using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Kobalt.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLogChannels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "log_channel",
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
                    table.PrimaryKey("pk_log_channel", x => x.id);
                    table.ForeignKey(
                        name: "fk_log_channel_guild_guild_id",
                        column: x => x.guild_id,
                        principalSchema: "kobalt_core",
                        principalTable: "guild",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_log_channel_guild_id",
                schema: "kobalt_core",
                table: "log_channel",
                column: "guild_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "log_channel",
                schema: "kobalt_core");
        }
    }
}
