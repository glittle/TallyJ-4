using Microsoft.EntityFrameworkCore.Migrations;
namespace Backend.EF.Migrations


#nullable disable

{
    /// <inheritdoc />
    public partial class AddTokenHashToRefreshToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TokenHash",
                table: "RefreshToken",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TokenHash",
                table: "RefreshToken");
        }
    }
}



