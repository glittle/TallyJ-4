using Microsoft.EntityFrameworkCore.Migrations;
namespace Backend.EF.Migrations


#nullable disable

{
    /// <inheritdoc />
    public partial class UpdatePendingChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Computer_Location",
                table: "Computers");

            migrationBuilder.AddForeignKey(
                name: "FK_Computer_Location",
                table: "Computers",
                column: "LocationGuid",
                principalTable: "Locations",
                principalColumn: "LocationGuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Computer_Location",
                table: "Computers");

            migrationBuilder.AddForeignKey(
                name: "FK_Computer_Location",
                table: "Computers",
                column: "LocationGuid",
                principalTable: "Locations",
                principalColumn: "LocationGuid",
                onDelete: ReferentialAction.Cascade);
        }
    }
}



