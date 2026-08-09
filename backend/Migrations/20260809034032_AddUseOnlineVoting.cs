using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddUseOnlineVoting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "UseOnlineVoting",
                table: "Elections",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Existing elections with an online window were already "using" online voting.
            migrationBuilder.Sql(
                """
                UPDATE Elections
                SET UseOnlineVoting = 1
                WHERE OnlineWhenOpen IS NOT NULL OR OnlineWhenClose IS NOT NULL
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UseOnlineVoting",
                table: "Elections");
        }
    }
}
