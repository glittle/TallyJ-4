using Backend.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    [DbContext(typeof(MainDbContext))]
    [Migration("20260913160000_AddGuestTellersCanAddPeople")]
    /// <inheritdoc />
    public partial class AddGuestTellersCanAddPeople : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "GuestTellersCanAddPeople",
                table: "Elections",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GuestTellersCanAddPeople",
                table: "Elections");
        }
    }
}
