using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class DropElectionCanVoteCanReceive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanReceive",
                table: "Elections");

            migrationBuilder.DropColumn(
                name: "CanVote",
                table: "Elections");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CanReceive",
                table: "Elections",
                type: "varchar(1)",
                unicode: false,
                maxLength: 1,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CanVote",
                table: "Elections",
                type: "varchar(1)",
                unicode: false,
                maxLength: 1,
                nullable: true);
        }
    }
}
