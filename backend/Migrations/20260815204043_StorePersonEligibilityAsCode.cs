using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class StorePersonEligibilityAsCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IneligibleReasonCode",
                table: "People",
                type: "varchar(10)",
                unicode: false,
                maxLength: 10,
                nullable: true);

            ConvertGuidsToCodes(migrationBuilder);

            migrationBuilder.DropIndex(
                name: "IX_Person_CanVote",
                table: "People");

            migrationBuilder.DropColumn(
                name: "IneligibleReasonGuid",
                table: "People");

            migrationBuilder.CreateIndex(
                name: "IX_Person_CanVote",
                table: "People",
                columns: new[] { "CanVote", "IneligibleReasonCode" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "IneligibleReasonGuid",
                table: "People",
                type: "uniqueidentifier",
                nullable: true);

            ConvertCodesToGuids(migrationBuilder);

            migrationBuilder.DropIndex(
                name: "IX_Person_CanVote",
                table: "People");

            migrationBuilder.DropColumn(
                name: "IneligibleReasonCode",
                table: "People");

            migrationBuilder.CreateIndex(
                name: "IX_Person_CanVote",
                table: "People",
                columns: new[] { "CanVote", "IneligibleReasonGuid" });
        }

        private static void ConvertGuidsToCodes(MigrationBuilder migrationBuilder)
        {
            foreach (var (guid, code) in GuidToCodeMappings)
            {
                migrationBuilder.Sql(
                    $"UPDATE People SET IneligibleReasonCode = '{code}' WHERE {GuidEquals("IneligibleReasonGuid", guid, migrationBuilder)}");
            }
        }

        private static void ConvertCodesToGuids(MigrationBuilder migrationBuilder)
        {
            foreach (var (guid, code) in CanonicalCodeToGuidMappings)
            {
                migrationBuilder.Sql(
                    $"UPDATE People SET IneligibleReasonGuid = '{guid}' WHERE IneligibleReasonCode = '{code}'");
            }
        }

        private static string GuidEquals(string column, string guid, MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
            {
                return $"lower(cast({column} as text)) = '{guid.ToLowerInvariant()}'";
            }

            return $"{column} = '{guid}'";
        }

        // Canonical reasons plus legacy v3 sub-reason GUIDs.
        private static readonly (string Guid, string Code)[] GuidToCodeMappings =
        [
            ("D227534D-D7E8-E011-A095-002269C41D11", "X01"),
            ("CF27534D-D7E8-E011-A095-002269C41D11", "X02"),
            ("2add3a15-ec2d-437c-916f-7c581e693baa", "X03"),
            ("D127534D-D7E8-E011-A095-002269C41D11", "X04"),
            ("32e44592-a7d8-408a-b169-8871800f62aa", "X05"),
            ("D327534D-D7E8-E011-A095-002269C41D11", "X06"),
            ("D027534D-D7E8-E011-A095-002269C41D11", "X07"),
            ("E027534D-D7E8-E011-A095-002269C41D11", "X08"),
            ("D527534D-D7E8-E011-A095-002269C41D11", "X09"),
            ("e6dd1cdd-5da0-4222-9f17-f02ce6313b0a", "V01"),
            ("C05EAE49-B01B-E111-A7FB-002269C41D11", "V02"),
            ("D427534D-D7E8-E011-A095-002269C41D11", "V03"),
            ("920A1A55-C4A5-42E5-9BCE-31756B6A20B9", "V04"),
            ("EB159A43-FB09-4FA9-AC12-3F451073010B", "V05"),
            ("24278180-fe1b-4604-9f86-d453b151d824", "V06"),
            ("4B2B0F32-4E14-43A4-9103-C5E9C81E8783", "R01"),
            ("84FA30C9-F007-44E8-B097-CCA430AAA3AA", "R02"),
            ("f4c7de9e-d487-49ae-9868-5cd208cd863a", "R03"),
            ("CE27534D-D7E8-E011-A095-002269C41D11", "U01"),
            ("CD27534D-D7E8-E011-A095-002269C41D11", "U02"),
            ("C927534D-D7E8-E011-A095-002269C41D11", "U01"),
            ("CB27534D-D7E8-E011-A095-002269C41D11", "U01"),
            ("CC27534D-D7E8-E011-A095-002269C41D11", "U01"),
            ("CA27534D-D7E8-E011-A095-002269C41D11", "U01"),
            ("C827534D-D7E8-E011-A095-002269C41D11", "U02"),
            ("C727534D-D7E8-E011-A095-002269C41D11", "U02"),
            ("C627534D-D7E8-E011-A095-002269C41D11", "U02"),
        ];

        private static readonly (string Guid, string Code)[] CanonicalCodeToGuidMappings =
        [
            ("D227534D-D7E8-E011-A095-002269C41D11", "X01"),
            ("CF27534D-D7E8-E011-A095-002269C41D11", "X02"),
            ("2add3a15-ec2d-437c-916f-7c581e693baa", "X03"),
            ("D127534D-D7E8-E011-A095-002269C41D11", "X04"),
            ("32e44592-a7d8-408a-b169-8871800f62aa", "X05"),
            ("D327534D-D7E8-E011-A095-002269C41D11", "X06"),
            ("D027534D-D7E8-E011-A095-002269C41D11", "X07"),
            ("E027534D-D7E8-E011-A095-002269C41D11", "X08"),
            ("D527534D-D7E8-E011-A095-002269C41D11", "X09"),
            ("e6dd1cdd-5da0-4222-9f17-f02ce6313b0a", "V01"),
            ("C05EAE49-B01B-E111-A7FB-002269C41D11", "V02"),
            ("D427534D-D7E8-E011-A095-002269C41D11", "V03"),
            ("920A1A55-C4A5-42E5-9BCE-31756B6A20B9", "V04"),
            ("EB159A43-FB09-4FA9-AC12-3F451073010B", "V05"),
            ("24278180-fe1b-4604-9f86-d453b151d824", "V06"),
            ("4B2B0F32-4E14-43A4-9103-C5E9C81E8783", "R01"),
            ("84FA30C9-F007-44E8-B097-CCA430AAA3AA", "R02"),
            ("f4c7de9e-d487-49ae-9868-5cd208cd863a", "R03"),
            ("CE27534D-D7E8-E011-A095-002269C41D11", "U01"),
            ("CD27534D-D7E8-E011-A095-002269C41D11", "U02"),
        ];
    }
}
