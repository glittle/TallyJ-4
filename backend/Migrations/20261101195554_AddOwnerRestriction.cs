using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnerRestriction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RejectedAt",
                table: "OwnerSendControls",
                type: "datetimeoffset(0)",
                precision: 0,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RestrictionStatus",
                table: "OwnerSendControls",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReviewNote",
                table: "OwnerSendControls",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReviewRequestedAt",
                table: "OwnerSendControls",
                type: "datetimeoffset(0)",
                precision: 0,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReviewedAt",
                table: "OwnerSendControls",
                type: "datetimeoffset(0)",
                precision: 0,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedByUserId",
                table: "OwnerSendControls",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StatusChangedAt",
                table: "OwnerSendControls",
                type: "datetimeoffset(0)",
                precision: 0,
                nullable: true);

            // Existing owners are trusted. Approved also means approved for paid sends.
            // Accounts with no row yet are the same people, so they get an approved row too.
            // A later restart does not run this again.
            migrationBuilder.Sql("""
                UPDATE OwnerSendControls
                SET RestrictionStatus = N'Approved',
                    PaidSendsApproved = 1,
                    ApprovedAt = COALESCE(ApprovedAt, SYSDATETIMEOFFSET()),
                    StatusChangedAt = SYSDATETIMEOFFSET()
                WHERE RestrictionStatus = N'';

                INSERT INTO OwnerSendControls
                    (UserId, PaidSendsApproved, ApprovedAt, SendsFrozen, RestrictionStatus, StatusChangedAt)
                SELECT TRY_CAST(u.Id AS uniqueidentifier),
                       1,
                       SYSDATETIMEOFFSET(),
                       0,
                       N'Approved',
                       SYSDATETIMEOFFSET()
                FROM AspNetUsers AS u
                WHERE TRY_CAST(u.Id AS uniqueidentifier) IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1
                      FROM OwnerSendControls AS existing
                      WHERE existing.UserId = TRY_CAST(u.Id AS uniqueidentifier));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "OwnerSendControls");

            migrationBuilder.DropColumn(
                name: "RestrictionStatus",
                table: "OwnerSendControls");

            migrationBuilder.DropColumn(
                name: "ReviewNote",
                table: "OwnerSendControls");

            migrationBuilder.DropColumn(
                name: "ReviewRequestedAt",
                table: "OwnerSendControls");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "OwnerSendControls");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                table: "OwnerSendControls");

            migrationBuilder.DropColumn(
                name: "StatusChangedAt",
                table: "OwnerSendControls");
        }
    }
}
