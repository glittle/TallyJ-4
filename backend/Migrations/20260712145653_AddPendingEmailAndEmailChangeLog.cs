using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingEmailAndEmailChangeLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PendingEmail",
                table: "AspNetUsers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingEmailCode",
                table: "AspNetUsers",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PendingEmailExpiry",
                table: "AspNetUsers",
                type: "datetimeoffset(0)",
                precision: 0,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingEmailToken",
                table: "AspNetUsers",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserEmailChangeLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    OldEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NewEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    ChangedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserEmailChangeLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserEmailChangeLogs_UserId_ChangedAt",
                table: "UserEmailChangeLogs",
                columns: new[] { "UserId", "ChangedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserEmailChangeLogs");

            migrationBuilder.DropColumn(
                name: "PendingEmail",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PendingEmailCode",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PendingEmailExpiry",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PendingEmailToken",
                table: "AspNetUsers");
        }
    }
}
