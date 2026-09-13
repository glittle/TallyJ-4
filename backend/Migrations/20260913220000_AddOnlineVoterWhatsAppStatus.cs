using Backend.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    [DbContext(typeof(MainDbContext))]
    [Migration("20260913220000_AddOnlineVoterWhatsAppStatus")]
    /// <inheritdoc />
    public partial class AddOnlineVoterWhatsAppStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WhatsAppStatus",
                table: "OnlineVoters",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WhatsAppStatus",
                table: "OnlineVoters");
        }
    }
}
