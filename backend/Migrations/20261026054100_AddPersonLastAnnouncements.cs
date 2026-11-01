using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonLastAnnouncements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PersonLastAnnouncements",
                columns: table => new
                {
                    _RowId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersonGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ElectionGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Channel = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Outcome = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    Error = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AttemptedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonLastAnnouncements", x => x._RowId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonLastAnnouncement_Election_Channel_Outcome",
                table: "PersonLastAnnouncements",
                columns: new[] { "ElectionGuid", "Channel", "Outcome" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonLastAnnouncement_Person",
                table: "PersonLastAnnouncements",
                column: "PersonGuid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PersonLastAnnouncements");
        }
    }
}
