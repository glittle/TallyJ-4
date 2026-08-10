using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class RemovePersonFullNameColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PersonElection",
                table: "People");

            migrationBuilder.DropColumn(
                name: "_FullName",
                table: "People");

            migrationBuilder.DropColumn(
                name: "_FullNameFL",
                table: "People");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "_FullName",
                table: "People",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "_FullNameFL",
                table: "People",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PersonElection",
                table: "People",
                columns: new[] { "ElectionGuid", "_FullName" });
        }
    }
}
