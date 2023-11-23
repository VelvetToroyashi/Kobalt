using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Kobalt.Bot.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLoggingChannels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_log_channels",
                schema: "kobalt_core",
                table: "log_channels");

            migrationBuilder.DropColumn(
                name: "id",
                schema: "kobalt_core",
                table: "log_channels");

            migrationBuilder.AddPrimaryKey(
                name: "pk_log_channels",
                schema: "kobalt_core",
                table: "log_channels",
                column: "channel_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_log_channels",
                schema: "kobalt_core",
                table: "log_channels");

            migrationBuilder.AddColumn<int>(
                name: "id",
                schema: "kobalt_core",
                table: "log_channels",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "pk_log_channels",
                schema: "kobalt_core",
                table: "log_channels",
                column: "id");
        }
    }
}
