using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class MergeLogsIntoSecurityAuditLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Logs");

            migrationBuilder.AddColumn<Guid>(
                name: "ElectionGuid",
                table: "SecurityAuditLogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OnlineVoterId",
                table: "SecurityAuditLogs",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditLogs_ElectionGuid",
                table: "SecurityAuditLogs",
                column: "ElectionGuid");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditLogs_OnlineVoterId",
                table: "SecurityAuditLogs",
                column: "OnlineVoterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SecurityAuditLogs_ElectionGuid",
                table: "SecurityAuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_SecurityAuditLogs_OnlineVoterId",
                table: "SecurityAuditLogs");

            migrationBuilder.DropColumn(
                name: "ElectionGuid",
                table: "SecurityAuditLogs");

            migrationBuilder.DropColumn(
                name: "OnlineVoterId",
                table: "SecurityAuditLogs");

            migrationBuilder.CreateTable(
                name: "Logs",
                columns: table => new
                {
                    _RowId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AsOf = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    ComputerCode = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: true),
                    Details = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
                    ElectionGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HostAndVersion = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
                    LocationGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VoterId = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logs", x => x._RowId);
                });

            migrationBuilder.CreateIndex(
                name: "IX__Log",
                table: "Logs",
                column: "AsOf");

            migrationBuilder.CreateIndex(
                name: "nci_msft_1__Log_154BF30FBBDD3CC74014282844F74DFE",
                table: "Logs",
                columns: new[] { "ElectionGuid", "LocationGuid" });
        }
    }
}
