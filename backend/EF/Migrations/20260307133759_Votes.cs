using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.EF.Migrations
{
    /// <inheritdoc />
    public partial class Votes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvalidReasonGuid",
                table: "Votes");

            migrationBuilder.RenameColumn(
                name: "StatusCode",
                table: "Votes",
                newName: "VoteStatus");

            migrationBuilder.AddColumn<string>(
                name: "IneligibleReasonCode",
                table: "Votes",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IneligibleReasonCode",
                table: "Votes");

            migrationBuilder.RenameColumn(
                name: "VoteStatus",
                table: "Votes",
                newName: "StatusCode");

            migrationBuilder.AddColumn<Guid>(
                name: "InvalidReasonGuid",
                table: "Votes",
                type: "uniqueidentifier",
                nullable: true);
        }
    }
}
