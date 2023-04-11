using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Kobalt.Infractions.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "kobalt_infractions");

            migrationBuilder.CreateTable(
                name: "infraction_rules",
                schema: "kobalt_infractions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guild_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    action_type = table.Column<int>(type: "integer", nullable: false),
                    match_time_span = table.Column<TimeSpan>(type: "interval", nullable: true),
                    match_value = table.Column<int>(type: "integer", nullable: false),
                    match_type = table.Column<int>(type: "integer", nullable: false),
                    action_duration = table.Column<TimeSpan>(type: "interval", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_infraction_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "infractions",
                schema: "kobalt_infractions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    referenced_id = table.Column<int>(type: "integer", nullable: true),
                    is_hidden = table.Column<bool>(type: "boolean", nullable: false),
                    is_processable = table.Column<bool>(type: "boolean", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    guild_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    moderator_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_updated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_infractions", x => x.id);
                    table.ForeignKey(
                        name: "fk_infractions_infractions_referenced_id",
                        column: x => x.referenced_id,
                        principalSchema: "kobalt_infractions",
                        principalTable: "infractions",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_infractions_referenced_id",
                schema: "kobalt_infractions",
                table: "infractions",
                column: "referenced_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "infraction_rules",
                schema: "kobalt_infractions");

            migrationBuilder.DropTable(
                name: "infractions",
                schema: "kobalt_infractions");
        }
    }
}
