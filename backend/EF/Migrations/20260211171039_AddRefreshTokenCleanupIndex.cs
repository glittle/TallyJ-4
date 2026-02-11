using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TallyJ4.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenCleanupIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_ExpiresAt_IsRevoked",
                table: "RefreshToken",
                columns: new[] { "ExpiresAt", "IsRevoked" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RefreshToken_ExpiresAt_IsRevoked",
                table: "RefreshToken");
        }
    }
}
