using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonKioskCodeUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PersonKioskCode",
                table: "People",
                columns: new[] { "ElectionGuid", "KioskCode" },
                unique: true,
                filter: "([KioskCode] IS NOT NULL AND [KioskCode]<>'')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PersonKioskCode",
                table: "People");
        }
    }
}