using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnerTestCodeLimit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastOwnerTestCodeAt",
                table: "OwnerSendControls",
                type: "datetimeoffset(0)",
                precision: 0,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerTestCodeCount",
                table: "OwnerSendControls",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "OwnerTestCodeUtcDate",
                table: "OwnerSendControls",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastOwnerTestCodeAt",
                table: "OwnerSendControls");

            migrationBuilder.DropColumn(
                name: "OwnerTestCodeCount",
                table: "OwnerSendControls");

            migrationBuilder.DropColumn(
                name: "OwnerTestCodeUtcDate",
                table: "OwnerSendControls");
        }
    }
}
