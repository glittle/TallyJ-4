using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddVoterContactFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExpectedPhoneRegions",
                table: "Elections",
                type: "varchar(80)",
                unicode: false,
                maxLength: 80,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VoterContactFlags",
                columns: table => new
                {
                    _RowId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ElectionGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceRowNumber = table.Column<int>(type: "int", nullable: true),
                    MaskedValue = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ContactKey = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    FlaggedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoterContactFlags", x => x._RowId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VoterContactFlag_Election_Active",
                table: "VoterContactFlags",
                columns: new[] { "ElectionGuid", "Active" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VoterContactFlags");

            migrationBuilder.DropColumn(
                name: "ExpectedPhoneRegions",
                table: "Elections");
        }
    }
}
