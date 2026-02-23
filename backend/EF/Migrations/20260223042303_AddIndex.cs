using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PersonBahaiID",
                table: "People",
                columns: new[] { "ElectionGuid", "BahaiId" },
                unique: true,
                filter: "([BahaiId] IS NOT NULL AND [BahaiId]<>'')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PersonBahaiID",
                table: "People");
        }
    }
}
