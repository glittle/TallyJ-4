using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.EF.Migrations
{
    /// <inheritdoc />
    public partial class LocationTypeCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LocationTypeCode",
                table: "Locations",
                type: "varchar(15)",
                unicode: false,
                maxLength: 15,
                nullable: true);

            // Backfill LocationTypeCode for existing known sentinel locations to avoid creating duplicates
            // or excluding them from reports that filter by LocationTypeCode.
            migrationBuilder.Sql(@"
                UPDATE Locations
                SET LocationTypeCode = 'Online'
                WHERE Name = 'Online' AND LocationTypeCode IS NULL;
            ");

            migrationBuilder.Sql(@"
                UPDATE Locations
                SET LocationTypeCode = 'Imported'
                WHERE Name = 'Imported' AND LocationTypeCode IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LocationTypeCode",
                table: "Locations");
        }
    }
}
