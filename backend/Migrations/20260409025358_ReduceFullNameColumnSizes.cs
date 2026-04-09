using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class ReduceFullNameColumnSizes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "TokenGuid",
                table: "TwoFactorToken",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldDefaultValueSql: "(CONVERT([uniqueidentifier],CONVERT([binary](10),newid(),(0))+CONVERT([binary](6),getdate(),(0)),(0)))");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Timestamp",
                table: "SecurityAuditLogs",
                type: "datetime2(2)",
                precision: 2,
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2(2)",
                oldPrecision: 2,
                oldDefaultValueSql: "(getdate())");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "RefreshToken",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "(getdate())");

            migrationBuilder.AlterColumn<long>(
                name: "_RowVersionInt",
                table: "People",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true,
                oldComputedColumnSql: "(CONVERT([bigint],[_RowVersion],(0)))");

            migrationBuilder.AlterColumn<string>(
                name: "_FullNameFL",
                table: "People",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(460)",
                oldMaxLength: 460,
                oldNullable: true,
                oldComputedColumnSql: "((((coalesce([FirstName]+' ','')+[LastName])+coalesce((' ['+nullif([OtherNames],''))+']',''))+coalesce((' ['+nullif([OtherLastNames],''))+']',''))+coalesce((' ('+nullif([OtherInfo],''))+')',''))");

            migrationBuilder.AlterColumn<string>(
                name: "_FullName",
                table: "People",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(461)",
                oldMaxLength: 461,
                oldNullable: true,
                oldComputedColumnSql: "((((([LastName]+coalesce((' ['+nullif([OtherLastNames],''))+']',''))+', ')+coalesce([FirstName],''))+coalesce((' ['+nullif([OtherNames],''))+']',''))+coalesce((' ('+nullif([OtherInfo],''))+')',''))");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AsOf",
                table: "Logs",
                type: "datetime2(2)",
                precision: 2,
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2(2)",
                oldPrecision: 2,
                oldDefaultValueSql: "(getdate())");

            migrationBuilder.AlterColumn<bool>(
                name: "HasContent",
                table: "ImportFiles",
                type: "bit",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true,
                oldComputedColumnSql: "(CONVERT([bit],case when [Contents] IS NULL then (0) else (1) end,(0)))");

            migrationBuilder.AlterColumn<int>(
                name: "FileSize",
                table: "ImportFiles",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true,
                oldComputedColumnSql: "(datalength([Contents]))");

            migrationBuilder.AlterColumn<Guid>(
                name: "ElectionGuid",
                table: "Elections",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldDefaultValueSql: "(CONVERT([uniqueidentifier],CONVERT([binary](10),newid(),(0))+CONVERT([binary](6),getdate(),(0)),(0)))");

            migrationBuilder.AlterColumn<DateTime>(
                name: "RegisteredAt",
                table: "Computers",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldDefaultValueSql: "(getdate())");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastActivity",
                table: "Computers",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldDefaultValueSql: "(getdate())");

            migrationBuilder.AlterColumn<string>(
                name: "_BallotCode",
                table: "Ballots",
                type: "varchar(32)",
                unicode: false,
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(32)",
                oldUnicode: false,
                oldMaxLength: 32,
                oldNullable: true,
                oldComputedColumnSql: "([ComputerCode]+CONVERT([varchar],[BallotNumAtComputer],(0)))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "TokenGuid",
                table: "TwoFactorToken",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "(CONVERT([uniqueidentifier],CONVERT([binary](10),newid(),(0))+CONVERT([binary](6),getdate(),(0)),(0)))",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Timestamp",
                table: "SecurityAuditLogs",
                type: "datetime2(2)",
                precision: 2,
                nullable: false,
                defaultValueSql: "(getdate())",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(2)",
                oldPrecision: 2);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "RefreshToken",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "(getdate())",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AsOf",
                table: "Logs",
                type: "datetime2(2)",
                precision: 2,
                nullable: false,
                defaultValueSql: "(getdate())",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(2)",
                oldPrecision: 2);

            migrationBuilder.AlterColumn<Guid>(
                name: "ElectionGuid",
                table: "Elections",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "(CONVERT([uniqueidentifier],CONVERT([binary](10),newid(),(0))+CONVERT([binary](6),getdate(),(0)),(0)))",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<DateTime>(
                name: "RegisteredAt",
                table: "Computers",
                type: "datetime2",
                nullable: true,
                defaultValueSql: "(getdate())",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastActivity",
                table: "Computers",
                type: "datetime2",
                nullable: true,
                defaultValueSql: "(getdate())",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "_RowVersionInt",
                table: "People",
                type: "bigint",
                nullable: true,
                computedColumnSql: "(CONVERT([bigint],[_RowVersion],(0)))",
                stored: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "_FullNameFL",
                table: "People",
                type: "nvarchar(460)",
                maxLength: 460,
                nullable: true,
                computedColumnSql: "((((coalesce([FirstName]+' ','')+[LastName])+coalesce((' ['+nullif([OtherNames],''))+']',''))+coalesce((' ['+nullif([OtherLastNames],''))+']',''))+coalesce((' ('+nullif([OtherInfo],''))+')',''))",
                stored: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "_FullName",
                table: "People",
                type: "nvarchar(461)",
                maxLength: 461,
                nullable: true,
                computedColumnSql: "((((([LastName]+coalesce((' ['+nullif([OtherLastNames],''))+']',''))+', ')+coalesce([FirstName],''))+coalesce((' ['+nullif([OtherNames],''))+']',''))+coalesce((' ('+nullif([OtherInfo],''))+')',''))",
                stored: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "HasContent",
                table: "ImportFiles",
                type: "bit",
                nullable: true,
                computedColumnSql: "(CONVERT([bit],case when [Contents] IS NULL then (0) else (1) end,(0)))",
                stored: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "FileSize",
                table: "ImportFiles",
                type: "int",
                nullable: true,
                computedColumnSql: "(datalength([Contents]))",
                stored: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "_BallotCode",
                table: "Ballots",
                type: "varchar(32)",
                unicode: false,
                maxLength: 32,
                nullable: true,
                computedColumnSql: "([ComputerCode]+CONVERT([varchar],[BallotNumAtComputer],(0)))",
                stored: true,
                oldClrType: typeof(string),
                oldType: "varchar(32)",
                oldUnicode: false,
                oldMaxLength: 32,
                oldNullable: true);
        }
    }
}
