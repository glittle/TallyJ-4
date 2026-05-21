using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class FixElectionStageAndLocationTallyStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Elections table: rename column (preserves data), adjust type/default, normalize values
            migrationBuilder.RenameColumn(
                name: "TallyStatus",
                table: "Elections",
                newName: "ElectionStage");

            migrationBuilder.AlterColumn<string>(
                name: "ElectionStage",
                table: "Elections",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: false,
                defaultValue: "SettingUp",
                oldClrType: typeof(string),
                oldType: "varchar(15)",
                oldUnicode: false,
                oldMaxLength: 15,
                oldNullable: true);

            migrationBuilder.Sql(@"
                UPDATE Elections SET ElectionStage = 'SettingUp'
                WHERE ElectionStage IS NULL
                   OR ElectionStage IN ('Setup', 'Draft');

                UPDATE Elections SET ElectionStage = 'GatheringBallots'
                WHERE ElectionStage IN ('Voting', 'Counting', 'Gathering');

                UPDATE Elections SET ElectionStage = 'ProcessingBallots'
                WHERE ElectionStage IN ('Tallying', 'Finalized', 'Processing', 'Archived', 'Complete', 'Deleted');
            ");

            // Locations table: rename column (preserves data), adjust type, normalize values
            migrationBuilder.RenameColumn(
                name: "TallyStatus",
                table: "Locations",
                newName: "LocationTallyStatus");

            migrationBuilder.AlterColumn<string>(
                name: "LocationTallyStatus",
                table: "Locations",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(15)",
                oldUnicode: false,
                oldMaxLength: 15,
                oldNullable: true);

            migrationBuilder.Sql(@"
                UPDATE Locations SET LocationTallyStatus = 'NotStarted'
                WHERE LocationTallyStatus IS NULL 
                   OR LocationTallyStatus NOT IN ('NotStarted', 'InProgress', 'Complete');
            ");

            // Remove the temporary default that was only needed during the rename + data migration
            // (matches the final model which has no default on ElectionStage)
            migrationBuilder.AlterColumn<string>(
                name: "ElectionStage",
                table: "Elections",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldUnicode: false,
                oldMaxLength: 20,
                oldDefaultValue: "SettingUp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse for Locations
            migrationBuilder.RenameColumn(
                name: "LocationTallyStatus",
                table: "Locations",
                newName: "TallyStatus");

            migrationBuilder.AlterColumn<string>(
                name: "TallyStatus",
                table: "Locations",
                type: "varchar(15)",
                unicode: false,
                maxLength: 15,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldUnicode: false,
                oldMaxLength: 20,
                oldNullable: true);

            // Reverse for Elections
            migrationBuilder.RenameColumn(
                name: "ElectionStage",
                table: "Elections",
                newName: "TallyStatus");

            migrationBuilder.AlterColumn<string>(
                name: "TallyStatus",
                table: "Elections",
                type: "varchar(15)",
                unicode: false,
                maxLength: 15,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldUnicode: false,
                oldMaxLength: 20,
                oldNullable: false,
                oldDefaultValue: "SettingUp");
        }
    }
}
