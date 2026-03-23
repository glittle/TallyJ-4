using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class InitialMar2026 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    GoogleId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TelegramId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuthMethod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PasswordResetToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PasswordResetExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Elections",
                columns: table => new
                {
                    _RowId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ElectionGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(CONVERT([uniqueidentifier],CONVERT([binary](10),newid(),(0))+CONVERT([binary](6),getdate(),(0)),(0)))"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Convenor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    DateOfElection = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    ElectionType = table.Column<string>(type: "varchar(5)", unicode: false, maxLength: 5, nullable: true),
                    ElectionMode = table.Column<string>(type: "varchar(1)", unicode: false, maxLength: 1, nullable: true),
                    NumberToElect = table.Column<int>(type: "int", nullable: true),
                    NumberExtra = table.Column<int>(type: "int", nullable: true),
                    CanVote = table.Column<string>(type: "varchar(1)", unicode: false, maxLength: 1, nullable: true),
                    CanReceive = table.Column<string>(type: "varchar(1)", unicode: false, maxLength: 1, nullable: true),
                    LastEnvNum = table.Column<int>(type: "int", nullable: true),
                    TallyStatus = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: true),
                    ShowFullReport = table.Column<bool>(type: "bit", nullable: true),
                    LinkedElectionGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LinkedElectionKind = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: true),
                    OwnerLoginId = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    ElectionPasscode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ListedForPublicAsOf = table.Column<DateTime>(type: "datetime2", nullable: true),
                    _RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    ListForPublic = table.Column<bool>(type: "bit", nullable: true),
                    ShowAsTest = table.Column<bool>(type: "bit", nullable: true),
                    UseCallInButton = table.Column<bool>(type: "bit", nullable: true),
                    HidePreBallotPages = table.Column<bool>(type: "bit", nullable: true),
                    MaskVotingMethod = table.Column<bool>(type: "bit", nullable: true),
                    OnlineWhenOpen = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    OnlineWhenClose = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    OnlineCloseIsEstimate = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    OnlineSelectionProcess = table.Column<string>(type: "varchar(1)", unicode: false, maxLength: 1, nullable: true),
                    OnlineAnnounced = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    EmailFromAddress = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    EmailFromName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EmailText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SmsText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EmailSubject = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CustomMethods = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    VotingMethods = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    Flags = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Elections", x => x._RowId);
                    table.UniqueConstraint("AK_Elections_ElectionGuid", x => x.ElectionGuid);
                });

            migrationBuilder.CreateTable(
                name: "Logs",
                columns: table => new
                {
                    _RowId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AsOf = table.Column<DateTime>(type: "datetime2(2)", precision: 2, nullable: false, defaultValueSql: "(getdate())"),
                    ElectionGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LocationGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VoterId = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ComputerCode = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: true),
                    Details = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
                    HostAndVersion = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logs", x => x._RowId);
                });

            migrationBuilder.CreateTable(
                name: "OnlineVoters",
                columns: table => new
                {
                    _RowId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VoterId = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    VoterIdType = table.Column<string>(type: "varchar(1)", unicode: false, maxLength: 1, nullable: false, defaultValue: "E"),
                    WhenRegistered = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    WhenLastLogin = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    EmailCodes = table.Column<string>(type: "varchar(25)", unicode: false, maxLength: 25, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OtherInfo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    VerifyCode = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: true),
                    VerifyCodeDate = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    VerifyAttempts = table.Column<int>(type: "int", nullable: true),
                    VerifyAttemptsStart = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnlineVoters", x => x._RowId);
                });

            migrationBuilder.CreateTable(
                name: "OnlineVotingInfos",
                columns: table => new
                {
                    _RowId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ElectionGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WhenBallotCreated = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    Status = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    WhenStatus = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    ListPool = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PoolLocked = table.Column<bool>(type: "bit", nullable: true),
                    HistoryStatus = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
                    NotifiedAboutOpening = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnlineVotingInfos", x => x._RowId);
                });

            migrationBuilder.CreateTable(
                name: "SecurityAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Timestamp = table.Column<DateTime>(type: "datetime2(2)", precision: 2, nullable: false, defaultValueSql: "(getdate())"),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Details = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsSuspicious = table.Column<bool>(type: "bit", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SmsLogs",
                columns: table => new
                {
                    _RowId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SmsSid = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    Phone = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    SentDate = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    ElectionGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PersonGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastStatus = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    LastDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorCode = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmsLogs", x => x._RowId);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshToken",
                columns: table => new
                {
                    _RowId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())"),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    RevokedReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReplacedByToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    _RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshToken", x => x._RowId);
                    table.ForeignKey(
                        name: "FK_RefreshToken_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TwoFactorToken",
                columns: table => new
                {
                    _RowId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TokenGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(CONVERT([uniqueidentifier],CONVERT([binary](10),newid(),(0))+CONVERT([binary](6),getdate(),(0)),(0)))"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Secret = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    _RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TwoFactorToken", x => x._RowId);
                    table.ForeignKey(
                        name: "FK_TwoFactorToken_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImportFiles",
                columns: table => new
                {
                    _RowId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ElectionGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadTime = table.Column<DateTime>(type: "datetime2(2)", precision: 2, nullable: true),
                    ImportTime = table.Column<DateTime>(type: "datetime2(2)", precision: 2, nullable: true),
                    FileSize = table.Column<int>(type: "int", nullable: true, computedColumnSql: "(datalength([Contents]))", stored: false),
                    HasContent = table.Column<bool>(type: "bit", nullable: true, computedColumnSql: "(CONVERT([bit],case when [Contents] IS NULL then (0) else (1) end,(0)))", stored: false),
                    FirstDataRow = table.Column<int>(type: "int", nullable: true),
                    ColumnsToRead = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OriginalFileName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProcessingStatus = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    FileType = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    CodePage = table.Column<int>(type: "int", nullable: true),
                    Messages = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
                    Contents = table.Column<byte[]>(type: "image", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportFiles", x => x._RowId);
                    table.ForeignKey(
                        name: "FK_ImportFile_Election",
                        column: x => x.ElectionGuid,
                        principalTable: "Elections",
                        principalColumn: "ElectionGuid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JoinElectionUsers",
                columns: table => new
                {
                    _RowId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ElectionGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    InviteEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    InviteWhen = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JoinElectionUsers", x => x._RowId);
                    table.ForeignKey(
                        name: "FK_JoinElectionUser_Election",
                        column: x => x.ElectionGuid,
                        principalTable: "Elections",
                        principalColumn: "ElectionGuid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    _RowId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ElectionGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ContactInfo = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Long = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Lat = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    TallyStatus = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: true),
                    BallotsCollected = table.Column<int>(type: "int", nullable: true),
                    LocationTypeCode = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VotingLocation", x => x._RowId);
                    table.UniqueConstraint("AK_Locations_LocationGuid", x => x.LocationGuid);
                    table.ForeignKey(
                        name: "FK_Location_Election",
                        column: x => x.ElectionGuid,
                        principalTable: "Elections",
                        principalColumn: "ElectionGuid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    _RowId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ElectionGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    _RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    AsOf = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x._RowId);
                    table.ForeignKey(
                        name: "FK_Message_Election",
                        column: x => x.ElectionGuid,
                        principalTable: "Elections",
                        principalColumn: "ElectionGuid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "People",
                columns: table => new
                {
                    _RowId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ElectionGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OtherLastNames = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OtherNames = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OtherInfo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Area = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BahaiId = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    CombinedInfo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CombinedSoundCodes = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
                    CombinedInfoAtStart = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AgeGroup = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: true),
                    CanVote = table.Column<bool>(type: "bit", nullable: true),
                    CanReceiveVotes = table.Column<bool>(type: "bit", nullable: true),
                    IneligibleReasonGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RegistrationTime = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    VotingLocationGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VotingMethod = table.Column<string>(type: "varchar(1)", unicode: false, maxLength: 1, nullable: true),
                    EnvNum = table.Column<int>(type: "int", nullable: true),
                    _RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    _FullName = table.Column<string>(type: "nvarchar(461)", maxLength: 461, nullable: true, computedColumnSql: "((((([LastName]+coalesce((' ['+nullif([OtherLastNames],''))+']',''))+', ')+coalesce([FirstName],''))+coalesce((' ['+nullif([OtherNames],''))+']',''))+coalesce((' ('+nullif([OtherInfo],''))+')',''))", stored: true),
                    _RowVersionInt = table.Column<long>(type: "bigint", nullable: true, computedColumnSql: "(CONVERT([bigint],[_RowVersion],(0)))", stored: false),
                    _FullNameFL = table.Column<string>(type: "nvarchar(460)", maxLength: 460, nullable: true, computedColumnSql: "((((coalesce([FirstName]+' ','')+[LastName])+coalesce((' ['+nullif([OtherNames],''))+']',''))+coalesce((' ['+nullif([OtherLastNames],''))+']',''))+coalesce((' ('+nullif([OtherInfo],''))+')',''))", stored: true),
                    Teller1 = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    Teller2 = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Phone = table.Column<string>(type: "varchar(25)", unicode: false, maxLength: 25, nullable: true),
                    HasOnlineBallot = table.Column<bool>(type: "bit", nullable: true),
                    Flags = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    UnitName = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    KioskCode = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    RegistrationHistory = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_People", x => x._RowId);
                    table.UniqueConstraint("AK_People_PersonGuid", x => x.PersonGuid);
                    table.ForeignKey(
                        name: "FK_Person_Election",
                        column: x => x.ElectionGuid,
                        principalTable: "Elections",
                        principalColumn: "ElectionGuid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResultSummaries",
                columns: table => new
                {
                    _RowId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ElectionGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResultType = table.Column<string>(type: "char(1)", unicode: false, fixedLength: true, maxLength: 1, nullable: false),
                    UseOnReports = table.Column<bool>(type: "bit", nullable: true),
                    NumVoters = table.Column<int>(type: "int", nullable: true),
                    NumEligibleToVote = table.Column<int>(type: "int", nullable: true),
                    MailedInBallots = table.Column<int>(type: "int", nullable: true),
                    DroppedOffBallots = table.Column<int>(type: "int", nullable: true),
                    InPersonBallots = table.Column<int>(type: "int", nullable: true),
                    SpoiledBallots = table.Column<int>(type: "int", nullable: true),
                    SpoiledVotes = table.Column<int>(type: "int", nullable: true),
                    TotalVotes = table.Column<int>(type: "int", nullable: true),
                    BallotsReceived = table.Column<int>(type: "int", nullable: true),
                    BallotsNeedingReview = table.Column<int>(type: "int", nullable: true),
                    CalledInBallots = table.Column<int>(type: "int", nullable: true),
                    OnlineBallots = table.Column<int>(type: "int", nullable: true),
                    SpoiledManualBallots = table.Column<int>(type: "int", nullable: true),
                    Custom1Ballots = table.Column<int>(type: "int", nullable: true),
                    Custom2Ballots = table.Column<int>(type: "int", nullable: true),
                    Custom3Ballots = table.Column<int>(type: "int", nullable: true),
                    ImportedBallots = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResultSummaries", x => x._RowId);
                    table.ForeignKey(
                        name: "FK_ResultSummary_Election1",
                        column: x => x.ElectionGuid,
                        principalTable: "Elections",
                        principalColumn: "ElectionGuid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResultTies",
                columns: table => new
                {
                    _RowId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ElectionGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TieBreakGroup = table.Column<int>(type: "int", nullable: false),
                    TieBreakRequired = table.Column<bool>(type: "bit", nullable: true),
                    NumToElect = table.Column<int>(type: "int", nullable: false),
                    NumInTie = table.Column<int>(type: "int", nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResultTies", x => x._RowId);
                    table.ForeignKey(
                        name: "FK_ResultTie_Election",
                        column: x => x.ElectionGuid,
                        principalTable: "Elections",
                        principalColumn: "ElectionGuid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tellers",
                columns: table => new
                {
                    _RowId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ElectionGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UsingComputerCode = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: true),
                    IsHeadTeller = table.Column<bool>(type: "bit", nullable: true),
                    _RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tellers", x => x._RowId);
                    table.ForeignKey(
                        name: "FK_Teller_Election",
                        column: x => x.ElectionGuid,
                        principalTable: "Elections",
                        principalColumn: "ElectionGuid");
                });

            migrationBuilder.CreateTable(
                name: "Ballots",
                columns: table => new
                {
                    _RowId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocationGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BallotGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StatusCode = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    ComputerCode = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: false),
                    BallotNumAtComputer = table.Column<int>(type: "int", nullable: false),
                    _BallotCode = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: true, computedColumnSql: "([ComputerCode]+CONVERT([varchar],[BallotNumAtComputer],(0)))", stored: true),
                    Teller1 = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    Teller2 = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    _RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ballots", x => x._RowId);
                    table.UniqueConstraint("AK_Ballots_BallotGuid", x => x.BallotGuid);
                    table.ForeignKey(
                        name: "FK_Ballot_Location1",
                        column: x => x.LocationGuid,
                        principalTable: "Locations",
                        principalColumn: "LocationGuid",
                        onDelete: ReferentialAction.Cascade);
                });

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
                        principalColumn: "LocationGuid");
                });

            migrationBuilder.CreateTable(
                name: "Results",
                columns: table => new
                {
                    _RowId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ElectionGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoteCount = table.Column<int>(type: "int", nullable: true),
                    Rank = table.Column<int>(type: "int", nullable: false),
                    Section = table.Column<string>(type: "char(1)", unicode: false, fixedLength: true, maxLength: 1, nullable: false),
                    CloseToPrev = table.Column<bool>(type: "bit", nullable: true),
                    CloseToNext = table.Column<bool>(type: "bit", nullable: true),
                    IsTied = table.Column<bool>(type: "bit", nullable: true),
                    TieBreakGroup = table.Column<int>(type: "int", nullable: true),
                    TieBreakRequired = table.Column<bool>(type: "bit", nullable: true),
                    TieBreakCount = table.Column<int>(type: "int", nullable: true),
                    IsTieResolved = table.Column<bool>(type: "bit", nullable: true),
                    RankInExtra = table.Column<int>(type: "int", nullable: true),
                    ForceShowInOther = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Results", x => x._RowId);
                    table.ForeignKey(
                        name: "FK_Result_Election",
                        column: x => x.ElectionGuid,
                        principalTable: "Elections",
                        principalColumn: "ElectionGuid");
                    table.ForeignKey(
                        name: "FK_Result_Person",
                        column: x => x.PersonGuid,
                        principalTable: "People",
                        principalColumn: "PersonGuid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Votes",
                columns: table => new
                {
                    _RowId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BallotGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PositionOnBallot = table.Column<int>(type: "int", nullable: false),
                    PersonGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VoteStatus = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    IneligibleReasonCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SingleNameElectionCount = table.Column<int>(type: "int", nullable: true),
                    _RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    PersonCombinedInfo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OnlineVoteRaw = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Votes", x => x._RowId);
                    table.ForeignKey(
                        name: "FK_Vote_Ballot",
                        column: x => x.BallotGuid,
                        principalTable: "Ballots",
                        principalColumn: "BallotGuid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Vote_Person1",
                        column: x => x.PersonGuid,
                        principalTable: "People",
                        principalColumn: "PersonGuid");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Ballot",
                table: "Ballots",
                column: "BallotGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ballot_Code",
                table: "Ballots",
                column: "ComputerCode");

            migrationBuilder.CreateIndex(
                name: "IX_Ballot_Location",
                table: "Ballots",
                column: "LocationGuid");

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

            migrationBuilder.CreateIndex(
                name: "IX_Election",
                table: "Elections",
                column: "ElectionGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportFiles_ElectionGuid",
                table: "ImportFiles",
                column: "ElectionGuid");

            migrationBuilder.CreateIndex(
                name: "IX_JoinElectionUser_ElectionGuid",
                table: "JoinElectionUsers",
                column: "ElectionGuid");

            migrationBuilder.CreateIndex(
                name: "IX_JoinElectionUser_UserId",
                table: "JoinElectionUsers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Location",
                table: "Locations",
                column: "LocationGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Location_Election",
                table: "Locations",
                column: "ElectionGuid");

            migrationBuilder.CreateIndex(
                name: "IX__Log",
                table: "Logs",
                column: "AsOf");

            migrationBuilder.CreateIndex(
                name: "nci_msft_1__Log_154BF30FBBDD3CC74014282844F74DFE",
                table: "Logs",
                columns: new[] { "ElectionGuid", "LocationGuid" });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ElectionGuid",
                table: "Messages",
                column: "ElectionGuid");

            migrationBuilder.CreateIndex(
                name: "IX_OnlineVoter_Id",
                table: "OnlineVoters",
                column: "VoterId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OnlineVotingInfo_ElectionPerson",
                table: "OnlineVotingInfos",
                columns: new[] { "ElectionGuid", "PersonGuid" });

            migrationBuilder.CreateIndex(
                name: "IX_OnlineVotingInfo_Person",
                table: "OnlineVotingInfos",
                column: "PersonGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Person",
                table: "People",
                column: "ElectionGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Person_1",
                table: "People",
                column: "PersonGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Person_CanVote",
                table: "People",
                columns: new[] { "CanVote", "IneligibleReasonGuid" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonBahaiID",
                table: "People",
                columns: new[] { "ElectionGuid", "BahaiId" },
                unique: true,
                filter: "([BahaiId] IS NOT NULL AND [BahaiId]<>'')");

            migrationBuilder.CreateIndex(
                name: "IX_PersonElection",
                table: "People",
                columns: new[] { "ElectionGuid", "_FullName" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonEmail",
                table: "People",
                columns: new[] { "ElectionGuid", "Email" },
                unique: true,
                filter: "([Email] IS NOT NULL AND [Email]<>'')");

            migrationBuilder.CreateIndex(
                name: "IX_PersonPhone",
                table: "People",
                columns: new[] { "ElectionGuid", "Phone" },
                unique: true,
                filter: "([Phone] IS NOT NULL AND [Phone]<>'')");

            migrationBuilder.CreateIndex(
                name: "nci_msft_Person_22A77D9DC21D83B4582C43E94A27236D",
                table: "People",
                column: "ElectionGuid");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_UserId",
                table: "RefreshToken",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Result_Election",
                table: "Results",
                column: "ElectionGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Results_PersonGuid",
                table: "Results",
                column: "PersonGuid");

            migrationBuilder.CreateIndex(
                name: "Ix_ResultSummary_Election",
                table: "ResultSummaries",
                column: "ElectionGuid");

            migrationBuilder.CreateIndex(
                name: "IX_ResultTie",
                table: "ResultTies",
                columns: new[] { "ElectionGuid", "TieBreakGroup" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditLogs_EventType",
                table: "SecurityAuditLogs",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditLogs_IsSuspicious",
                table: "SecurityAuditLogs",
                column: "IsSuspicious");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditLogs_Timestamp",
                table: "SecurityAuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditLogs_UserId",
                table: "SecurityAuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SmsLog",
                table: "SmsLogs",
                column: "SmsSid");

            migrationBuilder.CreateIndex(
                name: "IX_SmsLog_Election_Date",
                table: "SmsLogs",
                columns: new[] { "ElectionGuid", "LastDate" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Teller",
                table: "Tellers",
                columns: new[] { "ElectionGuid", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TwoFactorToken_UserId",
                table: "TwoFactorToken",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VoteBallot",
                table: "Votes",
                columns: new[] { "BallotGuid", "PositionOnBallot" });

            migrationBuilder.CreateIndex(
                name: "IX_VotePerson",
                table: "Votes",
                column: "PersonGuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "Computers");

            migrationBuilder.DropTable(
                name: "ImportFiles");

            migrationBuilder.DropTable(
                name: "JoinElectionUsers");

            migrationBuilder.DropTable(
                name: "Logs");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "OnlineVoters");

            migrationBuilder.DropTable(
                name: "OnlineVotingInfos");

            migrationBuilder.DropTable(
                name: "RefreshToken");

            migrationBuilder.DropTable(
                name: "Results");

            migrationBuilder.DropTable(
                name: "ResultSummaries");

            migrationBuilder.DropTable(
                name: "ResultTies");

            migrationBuilder.DropTable(
                name: "SecurityAuditLogs");

            migrationBuilder.DropTable(
                name: "SmsLogs");

            migrationBuilder.DropTable(
                name: "Tellers");

            migrationBuilder.DropTable(
                name: "TwoFactorToken");

            migrationBuilder.DropTable(
                name: "Votes");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Ballots");

            migrationBuilder.DropTable(
                name: "People");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropTable(
                name: "Elections");
        }
    }
}
