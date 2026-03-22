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
