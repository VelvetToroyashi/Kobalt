using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kobalt.Infractions.Data.Migrations
{
    /// <inheritdoc />
    public partial class InfractionRuleNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "rule_name",
                schema: "kobalt_infractions",
                table: "infraction_rules",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "rule_name",
                schema: "kobalt_infractions",
                table: "infraction_rules");
        }
    }
}
