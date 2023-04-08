using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Kobalt.ReminderService.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Kobalt.ReminderService");

            migrationBuilder.CreateTable(
                name: "Reminders",
                schema: "Kobalt.ReminderService",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AuthorID = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    ChannelID = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    GuildID = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    ReplyContent = table.Column<string>(type: "text", nullable: false),
                    Creation = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Expiration = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReplyMessageID = table.Column<decimal>(type: "numeric(20,0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reminders", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reminders",
                schema: "Kobalt.ReminderService");
        }
    }
}
