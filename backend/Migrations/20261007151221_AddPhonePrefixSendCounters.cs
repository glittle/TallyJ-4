using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPhonePrefixSendCounters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PhonePrefixSendCounters",
                columns: table => new
                {
                    Prefix = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    BucketStartedUnix = table.Column<long>(type: "bigint", nullable: false),
                    SendCount = table.Column<int>(type: "int", nullable: false),
                    PreviousCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhonePrefixSendCounters", x => x.Prefix);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhonePrefixSendCounters");
        }
    }
}
