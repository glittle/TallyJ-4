using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TallyJ4.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddComputerEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Computers",
                columns: table => new
                {
                    _RowId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ElectionGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComputerGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComputerCode = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: false),
                    BrowserInfo = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LastActivity = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    RegisteredAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    IsActive = table.Column<bool>(type: "bit", nullable: true, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Computers", x => x._RowId);
                    table.ForeignKey(
                        name: "FK_Computer_Election",
                        column: x => x.ElectionGuid,
                        principalTable: "Elections",
                        principalColumn: "ElectionGuid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Computer_Location",
                        column: x => x.LocationGuid,
                        principalTable: "Locations",
                        principalColumn: "LocationGuid",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Computer",
                table: "Computers",
                column: "ComputerGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Computer_Code",
                table: "Computers",
                columns: new[] { "ElectionGuid", "ComputerCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Computer_Location",
                table: "Computers",
                column: "LocationGuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Computers");
        }
    }
}
