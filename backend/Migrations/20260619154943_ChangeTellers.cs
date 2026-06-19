using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class ChangeTellers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsHeadTeller",
                table: "Tellers");

            migrationBuilder.DropColumn(
                name: "UsingComputerCode",
                table: "Tellers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsHeadTeller",
                table: "Tellers",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsingComputerCode",
                table: "Tellers",
                type: "varchar(2)",
                unicode: false,
                maxLength: 2,
                nullable: true);
        }
    }
}
