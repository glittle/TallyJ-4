using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPaidSendControls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AbuseAlertStates",
                columns: table => new
                {
                    AlertKey = table.Column<string>(type: "varchar(160)", unicode: false, maxLength: 160, nullable: false),
                    LastSentAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbuseAlertStates", x => x.AlertKey);
                });

            migrationBuilder.CreateTable(
                name: "CodeSendLogs",
                columns: table => new
                {
                    _RowId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ElectionGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Channel = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    MaskedDestination = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    Outcome = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeSendLogs", x => x._RowId);
                });

            migrationBuilder.CreateTable(
                name: "ElectionSendControls",
                columns: table => new
                {
                    ElectionGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaidSendsUsed = table.Column<int>(type: "int", nullable: false),
                    AllowanceOverride = table.Column<int>(type: "int", nullable: true),
                    SendsFrozen = table.Column<bool>(type: "bit", nullable: false),
                    Flagged = table.Column<bool>(type: "bit", nullable: false),
                    FlaggedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    FlaggedEntryCount = table.Column<int>(type: "int", nullable: false),
                    ClearedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    ClearedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectionSendControls", x => x.ElectionGuid);
                });

            migrationBuilder.CreateTable(
                name: "OwnerDailyPaidSends",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UtcDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SendCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwnerDailyPaidSends", x => new { x.UserId, x.UtcDate });
                });

            migrationBuilder.CreateTable(
                name: "OwnerSendControls",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaidSendsApproved = table.Column<bool>(type: "bit", nullable: false),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    ApprovedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DailyCapOverride = table.Column<int>(type: "int", nullable: true),
                    SendsFrozen = table.Column<bool>(type: "bit", nullable: false),
                    FirstPaidSendAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwnerSendControls", x => x.UserId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CodeSendLog_Election_SentAt",
                table: "CodeSendLogs",
                columns: new[] { "ElectionGuid", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CodeSendLog_Owner_SentAt",
                table: "CodeSendLogs",
                columns: new[] { "OwnerUserId", "SentAt" });

            // One-time approval for accounts that already have an election with online voting.
            // Owner and Admin join rows are both included because creating an election stores Admin.
            // OwnerLoginId is matched to a login email when the id is a GUID.
            // An existing control row is not updated.
            migrationBuilder.Sql("""
                INSERT INTO OwnerSendControls (UserId, PaidSendsApproved, ApprovedAt, SendsFrozen)
                SELECT DISTINCT j.UserId, CAST(1 AS bit), SYSUTCDATETIME(), CAST(0 AS bit)
                FROM JoinElectionUsers AS j
                INNER JOIN Elections AS e ON e.ElectionGuid = j.ElectionGuid
                WHERE e.UseOnlineVoting = 1
                  AND j.Role IN (N'Owner', N'Admin')
                  AND NOT EXISTS (
                      SELECT 1 FROM OwnerSendControls AS existing WHERE existing.UserId = j.UserId);

                INSERT INTO OwnerSendControls (UserId, PaidSendsApproved, ApprovedAt, SendsFrozen)
                SELECT DISTINCT TRY_CAST(u.Id AS uniqueidentifier), CAST(1 AS bit), SYSUTCDATETIME(), CAST(0 AS bit)
                FROM Elections AS e
                INNER JOIN AspNetUsers AS u ON LOWER(u.Email) = LOWER(e.OwnerLoginId)
                WHERE e.UseOnlineVoting = 1
                  AND e.OwnerLoginId IS NOT NULL
                  AND TRY_CAST(u.Id AS uniqueidentifier) IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1
                      FROM OwnerSendControls AS existing
                      WHERE existing.UserId = TRY_CAST(u.Id AS uniqueidentifier));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AbuseAlertStates");

            migrationBuilder.DropTable(
                name: "CodeSendLogs");

            migrationBuilder.DropTable(
                name: "ElectionSendControls");

            migrationBuilder.DropTable(
                name: "OwnerDailyPaidSends");

            migrationBuilder.DropTable(
                name: "OwnerSendControls");
        }
    }
}
