using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kobalt.Plugins.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class SetSchemaAndTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_guild_anti_raid_config_guilds_guild_id",
                schema: "kobalt_core",
                table: "guild_anti_raid_config");

            migrationBuilder.DropForeignKey(
                name: "fk_guild_auto_mod_config_guilds_guild_id",
                schema: "kobalt_core",
                table: "guild_auto_mod_config");

            migrationBuilder.DropForeignKey(
                name: "fk_guild_phishing_config_guilds_guild_id",
                schema: "kobalt_core",
                table: "guild_phishing_config");

            migrationBuilder.DropForeignKey(
                name: "fk_guild_user_joiner_guilds_guild_id",
                schema: "kobalt_core",
                table: "guild_user_joiner");

            migrationBuilder.DropForeignKey(
                name: "fk_guild_user_joiner_users_user_id",
                schema: "kobalt_core",
                table: "guild_user_joiner");

            migrationBuilder.DropPrimaryKey(
                name: "pk_guild_user_joiner",
                schema: "kobalt_core",
                table: "guild_user_joiner");

            migrationBuilder.DropPrimaryKey(
                name: "pk_guild_phishing_config",
                schema: "kobalt_core",
                table: "guild_phishing_config");

            migrationBuilder.DropPrimaryKey(
                name: "pk_guild_auto_mod_config",
                schema: "kobalt_core",
                table: "guild_auto_mod_config");

            migrationBuilder.DropPrimaryKey(
                name: "pk_guild_anti_raid_config",
                schema: "kobalt_core",
                table: "guild_anti_raid_config");

            migrationBuilder.RenameTable(
                name: "guild_user_joiner",
                schema: "kobalt_core",
                newName: "guild_user_joiners",
                newSchema: "kobalt_core");

            migrationBuilder.RenameTable(
                name: "guild_phishing_config",
                schema: "kobalt_core",
                newName: "guild_phishing_configs",
                newSchema: "kobalt_core");

            migrationBuilder.RenameTable(
                name: "guild_auto_mod_config",
                schema: "kobalt_core",
                newName: "guild_automod_configs",
                newSchema: "kobalt_core");

            migrationBuilder.RenameTable(
                name: "guild_anti_raid_config",
                schema: "kobalt_core",
                newName: "guild_anti_raid_configs",
                newSchema: "kobalt_core");

            migrationBuilder.RenameIndex(
                name: "ix_guild_user_joiner_user_id",
                schema: "kobalt_core",
                table: "guild_user_joiners",
                newName: "ix_guild_user_joiners_user_id");

            migrationBuilder.RenameIndex(
                name: "ix_guild_user_joiner_guild_id",
                schema: "kobalt_core",
                table: "guild_user_joiners",
                newName: "ix_guild_user_joiners_guild_id");

            migrationBuilder.RenameIndex(
                name: "ix_guild_phishing_config_guild_id",
                schema: "kobalt_core",
                table: "guild_phishing_configs",
                newName: "ix_guild_phishing_configs_guild_id");

            migrationBuilder.RenameIndex(
                name: "ix_guild_auto_mod_config_guild_id",
                schema: "kobalt_core",
                table: "guild_automod_configs",
                newName: "ix_guild_automod_configs_guild_id");

            migrationBuilder.RenameIndex(
                name: "ix_guild_anti_raid_config_guild_id",
                schema: "kobalt_core",
                table: "guild_anti_raid_configs",
                newName: "ix_guild_anti_raid_configs_guild_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_guild_user_joiners",
                schema: "kobalt_core",
                table: "guild_user_joiners",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_guild_phishing_configs",
                schema: "kobalt_core",
                table: "guild_phishing_configs",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_guild_automod_configs",
                schema: "kobalt_core",
                table: "guild_automod_configs",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_guild_anti_raid_configs",
                schema: "kobalt_core",
                table: "guild_anti_raid_configs",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_guild_anti_raid_configs_guilds_guild_id",
                schema: "kobalt_core",
                table: "guild_anti_raid_configs",
                column: "guild_id",
                principalSchema: "kobalt_core",
                principalTable: "guilds",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_guild_automod_configs_guilds_guild_id",
                schema: "kobalt_core",
                table: "guild_automod_configs",
                column: "guild_id",
                principalSchema: "kobalt_core",
                principalTable: "guilds",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_guild_phishing_configs_guilds_guild_id",
                schema: "kobalt_core",
                table: "guild_phishing_configs",
                column: "guild_id",
                principalSchema: "kobalt_core",
                principalTable: "guilds",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_guild_user_joiners_guilds_guild_id",
                schema: "kobalt_core",
                table: "guild_user_joiners",
                column: "guild_id",
                principalSchema: "kobalt_core",
                principalTable: "guilds",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_guild_user_joiners_users_user_id",
                schema: "kobalt_core",
                table: "guild_user_joiners",
                column: "user_id",
                principalSchema: "kobalt_core",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_guild_anti_raid_configs_guilds_guild_id",
                schema: "kobalt_core",
                table: "guild_anti_raid_configs");

            migrationBuilder.DropForeignKey(
                name: "fk_guild_automod_configs_guilds_guild_id",
                schema: "kobalt_core",
                table: "guild_automod_configs");

            migrationBuilder.DropForeignKey(
                name: "fk_guild_phishing_configs_guilds_guild_id",
                schema: "kobalt_core",
                table: "guild_phishing_configs");

            migrationBuilder.DropForeignKey(
                name: "fk_guild_user_joiners_guilds_guild_id",
                schema: "kobalt_core",
                table: "guild_user_joiners");

            migrationBuilder.DropForeignKey(
                name: "fk_guild_user_joiners_users_user_id",
                schema: "kobalt_core",
                table: "guild_user_joiners");

            migrationBuilder.DropPrimaryKey(
                name: "pk_guild_user_joiners",
                schema: "kobalt_core",
                table: "guild_user_joiners");

            migrationBuilder.DropPrimaryKey(
                name: "pk_guild_phishing_configs",
                schema: "kobalt_core",
                table: "guild_phishing_configs");

            migrationBuilder.DropPrimaryKey(
                name: "pk_guild_automod_configs",
                schema: "kobalt_core",
                table: "guild_automod_configs");

            migrationBuilder.DropPrimaryKey(
                name: "pk_guild_anti_raid_configs",
                schema: "kobalt_core",
                table: "guild_anti_raid_configs");

            migrationBuilder.RenameTable(
                name: "guild_user_joiners",
                schema: "kobalt_core",
                newName: "guild_user_joiner",
                newSchema: "kobalt_core");

            migrationBuilder.RenameTable(
                name: "guild_phishing_configs",
                schema: "kobalt_core",
                newName: "guild_phishing_config",
                newSchema: "kobalt_core");

            migrationBuilder.RenameTable(
                name: "guild_automod_configs",
                schema: "kobalt_core",
                newName: "guild_auto_mod_config",
                newSchema: "kobalt_core");

            migrationBuilder.RenameTable(
                name: "guild_anti_raid_configs",
                schema: "kobalt_core",
                newName: "guild_anti_raid_config",
                newSchema: "kobalt_core");

            migrationBuilder.RenameIndex(
                name: "ix_guild_user_joiners_user_id",
                schema: "kobalt_core",
                table: "guild_user_joiner",
                newName: "ix_guild_user_joiner_user_id");

            migrationBuilder.RenameIndex(
                name: "ix_guild_user_joiners_guild_id",
                schema: "kobalt_core",
                table: "guild_user_joiner",
                newName: "ix_guild_user_joiner_guild_id");

            migrationBuilder.RenameIndex(
                name: "ix_guild_phishing_configs_guild_id",
                schema: "kobalt_core",
                table: "guild_phishing_config",
                newName: "ix_guild_phishing_config_guild_id");

            migrationBuilder.RenameIndex(
                name: "ix_guild_automod_configs_guild_id",
                schema: "kobalt_core",
                table: "guild_auto_mod_config",
                newName: "ix_guild_auto_mod_config_guild_id");

            migrationBuilder.RenameIndex(
                name: "ix_guild_anti_raid_configs_guild_id",
                schema: "kobalt_core",
                table: "guild_anti_raid_config",
                newName: "ix_guild_anti_raid_config_guild_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_guild_user_joiner",
                schema: "kobalt_core",
                table: "guild_user_joiner",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_guild_phishing_config",
                schema: "kobalt_core",
                table: "guild_phishing_config",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_guild_auto_mod_config",
                schema: "kobalt_core",
                table: "guild_auto_mod_config",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_guild_anti_raid_config",
                schema: "kobalt_core",
                table: "guild_anti_raid_config",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_guild_anti_raid_config_guilds_guild_id",
                schema: "kobalt_core",
                table: "guild_anti_raid_config",
                column: "guild_id",
                principalSchema: "kobalt_core",
                principalTable: "guilds",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_guild_auto_mod_config_guilds_guild_id",
                schema: "kobalt_core",
                table: "guild_auto_mod_config",
                column: "guild_id",
                principalSchema: "kobalt_core",
                principalTable: "guilds",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_guild_phishing_config_guilds_guild_id",
                schema: "kobalt_core",
                table: "guild_phishing_config",
                column: "guild_id",
                principalSchema: "kobalt_core",
                principalTable: "guilds",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_guild_user_joiner_guilds_guild_id",
                schema: "kobalt_core",
                table: "guild_user_joiner",
                column: "guild_id",
                principalSchema: "kobalt_core",
                principalTable: "guilds",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_guild_user_joiner_users_user_id",
                schema: "kobalt_core",
                table: "guild_user_joiner",
                column: "user_id",
                principalSchema: "kobalt_core",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
