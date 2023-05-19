using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Kobalt.Phishing.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "kobalt_phishing");

            migrationBuilder.CreateTable(
                name: "suspicious_avatars",
                schema: "kobalt_phishing",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guild_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    md5hash = table.Column<string>(type: "text", nullable: true),
                    added_by = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    added_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    source = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<string>(type: "text", nullable: false),
                    phash = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_suspicious_avatars", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "suspicious_usernames",
                schema: "kobalt_phishing",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guild_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    username_pattern = table.Column<string>(type: "text", nullable: false),
                    parse_type = table.Column<int>(type: "integer", nullable: false),
                    created_by = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_suspicious_usernames", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "suspicious_avatars",
                schema: "kobalt_phishing");

            migrationBuilder.DropTable(
                name: "suspicious_usernames",
                schema: "kobalt_phishing");
        }
    }
}
