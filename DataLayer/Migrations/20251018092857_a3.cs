using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataLayer.Migrations
{
    /// <inheritdoc />
    public partial class a3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Leaderboard",
                columns: table => new
                {
                    LeaderboardID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StartDate = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    SeasonName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    SeasonNumber = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreateAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    UpdateAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Leaderbo__B358A1E60B6C8F5C", x => x.LeaderboardID);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    NotificationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Notifica__20CF2E12AAE7DD1E", x => x.NotificationId);
                });

            migrationBuilder.CreateTable(
                name: "Packages",
                columns: table => new
                {
                    PackageID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackageName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    DurationInDays = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Packages__322035ECD9DF94F4", x => x.PackageID);
                });

            migrationBuilder.CreateTable(
                name: "Passages",
                columns: table => new
                {
                    PassageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContentText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Passages__CC0F002C41B406F1", x => x.PassageId);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Roles__8AFACE1AA4681B10", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "Prompts",
                columns: table => new
                {
                    PromptId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PassageId = table.Column<int>(type: "int", nullable: true),
                    Skill = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PromptText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReferenceImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReferenceAudioUrl = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Prompts__456CA7536D5666A0", x => x.PromptId);
                    table.ForeignKey(
                        name: "FK_Prompts_Passages",
                        column: x => x.PassageId,
                        principalTable: "Passages",
                        principalColumn: "PassageId");
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RoleID = table.Column<int>(type: "int", nullable: false),
                    CurrentStreak = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    LastPracticeDate = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    AvatarUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Bio = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Users__1788CCACF48D0559", x => x.UserID);
                    table.ForeignKey(
                        name: "FK_Users_Roles",
                        column: x => x.RoleID,
                        principalTable: "Roles",
                        principalColumn: "RoleId");
                });

            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    AccountId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Username = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    PasswordHash = table.Column<string>(type: "varchar(512)", unicode: false, maxLength: 512, nullable: true),
                    AuthProvider = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    ProviderUserId = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    AccessToken = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
                    RefreshToken = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
                    TokenExpiresAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    Create_at = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    Update_at = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Accounts__349DA5A66E07EEBE", x => x.AccountId);
                    table.ForeignKey(
                        name: "FK_Accounts_Users",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "ArticleCategories",
                columns: table => new
                {
                    CategoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedByUserID = table.Column<int>(type: "int", nullable: true),
                    CreateAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UpdateAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ArticleC__19093A2B95CED098", x => x.CategoryID);
                    table.ForeignKey(
                        name: "FK_ArticleCategories_Users",
                        column: x => x.CreatedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    EventId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    CreateAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UpdateAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    CreateBy = table.Column<int>(type: "int", nullable: false),
                    UpdateBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Events__7944C8108B79DD0A", x => x.EventId);
                    table.ForeignKey(
                        name: "FK_Events_Users_Create",
                        column: x => x.CreateBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_Events_Users_Update",
                        column: x => x.UpdateBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "Exams",
                columns: table => new
                {
                    ExamId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdateBy = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UpdateAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Exams__297521C791F6DF8D", x => x.ExamId);
                    table.ForeignKey(
                        name: "FK_Exams_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_Exams_UpdatedBy",
                        column: x => x.UpdateBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetTokens",
                columns: table => new
                {
                    PasswordResetTokenID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CodeHash = table.Column<string>(type: "varchar(512)", unicode: false, maxLength: 512, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UsedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetTokens", x => x.PasswordResetTokenID);
                    table.ForeignKey(
                        name: "FK_PasswordResetTokens_Users",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    PaymentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    PackageID = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    PaymentGatewayTransactionID = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Payments__9B556A58D393F47D", x => x.PaymentID);
                    table.ForeignKey(
                        name: "FK_Payments_Packages",
                        column: x => x.PackageID,
                        principalTable: "Packages",
                        principalColumn: "PackageID");
                    table.ForeignKey(
                        name: "FK_Payments_Users",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    ReportID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SendBy = table.Column<int>(type: "int", nullable: false),
                    SendAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    ReplyBy = table.Column<int>(type: "int", nullable: true),
                    ReplyAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    ReplyContent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Reports__D5BD48E51D27B3D5", x => x.ReportID);
                    table.ForeignKey(
                        name: "FK_Reports_ReplyBy",
                        column: x => x.SendBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "Slides",
                columns: table => new
                {
                    SlideId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SlideUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SlideName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UpdateBy = table.Column<int>(type: "int", nullable: true),
                    CreateBy = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    UpdateAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    CreateAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Slides__9E7CB650BEF67F4A", x => x.SlideId);
                    table.ForeignKey(
                        name: "FK_Slides_Users_Create",
                        column: x => x.CreateBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_Slides_Users_Update",
                        column: x => x.UpdateBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "UserLeaderboard",
                columns: table => new
                {
                    UserLeaderboardID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    LeaderboardID = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__UserLead__F2E24551691C1D90", x => x.UserLeaderboardID);
                    table.ForeignKey(
                        name: "FK_UserLeaderboard_Leaderboard",
                        column: x => x.LeaderboardID,
                        principalTable: "Leaderboard",
                        principalColumn: "LeaderboardID");
                    table.ForeignKey(
                        name: "FK_UserLeaderboard_Users",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "UserNotifications",
                columns: table => new
                {
                    UniqueID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    NotificationId = table.Column<int>(type: "int", nullable: true),
                    CreateAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__UserNoti__A2A2BAAAD8BD5A4B", x => x.UniqueID);
                    table.ForeignKey(
                        name: "FK_UserNotifications_Notifications",
                        column: x => x.NotificationId,
                        principalTable: "Notifications",
                        principalColumn: "NotificationId");
                    table.ForeignKey(
                        name: "FK_UserNotifications_Users",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "VocabularyList",
                columns: table => new
                {
                    VocabularyListId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MakeBy = table.Column<int>(type: "int", nullable: false),
                    CreateAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UpdateAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    IsPublic = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Vocabula__C2D6E440F67079D4", x => x.VocabularyListId);
                    table.ForeignKey(
                        name: "FK_VocabularyList_Users",
                        column: x => x.MakeBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "Articles",
                columns: table => new
                {
                    ArticleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryID = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Articles__9C6270C88BD5B813", x => x.ArticleID);
                    table.ForeignKey(
                        name: "FK_Articles_ArticleCategories",
                        column: x => x.CategoryID,
                        principalTable: "ArticleCategories",
                        principalColumn: "CategoryID");
                    table.ForeignKey(
                        name: "FK_Articles_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_Articles_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "ExamAttempts",
                columns: table => new
                {
                    AttemptID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    ExamID = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    Score = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ListeningScore = table.Column<int>(type: "int", nullable: true),
                    ReadingScore = table.Column<int>(type: "int", nullable: true),
                    SpeakingScore = table.Column<int>(type: "int", nullable: true),
                    WritingScore = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ExamAtte__891A68864E80FEA1", x => x.AttemptID);
                    table.ForeignKey(
                        name: "FK_ExamAttempts_Exam",
                        column: x => x.ExamID,
                        principalTable: "Exams",
                        principalColumn: "ExamId");
                    table.ForeignKey(
                        name: "FK_ExamAttempts_Users",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "ExamParts",
                columns: table => new
                {
                    PartId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    PartCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    MaxQuestions = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ExamPart__7C3F0D501ED38AB6", x => x.PartId);
                    table.ForeignKey(
                        name: "FK_ExamParts_Exams",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "ExamId");
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    SubscriptionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    PackageID = table.Column<int>(type: "int", nullable: false),
                    PaymentID = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    EndTime = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Subscrip__9A2B24BDA2D67BCE", x => x.SubscriptionID);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Packages",
                        column: x => x.PackageID,
                        principalTable: "Packages",
                        principalColumn: "PackageID");
                    table.ForeignKey(
                        name: "FK_Subscriptions_Payments",
                        column: x => x.PaymentID,
                        principalTable: "Payments",
                        principalColumn: "PaymentID");
                    table.ForeignKey(
                        name: "FK_Subscriptions_Users",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "UserSpacedRepetition",
                columns: table => new
                {
                    UserSpacedRepetitionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    VocabularyListId = table.Column<int>(type: "int", nullable: false),
                    LastReviewedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    NextReviewAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    ReviewCount = table.Column<int>(type: "int", nullable: true),
                    Intervals = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__UserSpac__F141C818F2E5BE1C", x => x.UserSpacedRepetitionId);
                    table.ForeignKey(
                        name: "FK_UserSpacedRepetition_Users",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_UserSpacedRepetition_VocabularyList",
                        column: x => x.VocabularyListId,
                        principalTable: "VocabularyList",
                        principalColumn: "VocabularyListId");
                });

            migrationBuilder.CreateTable(
                name: "Vocabularies",
                columns: table => new
                {
                    VocabularyID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VocabularyListId = table.Column<int>(type: "int", nullable: false),
                    Word = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Definition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Example = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TypeOfWord = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Vocabula__9274069F7C141637", x => x.VocabularyID);
                    table.ForeignKey(
                        name: "FK_Vocabularies_VocabularyList",
                        column: x => x.VocabularyListId,
                        principalTable: "VocabularyList",
                        principalColumn: "VocabularyListId");
                });

            migrationBuilder.CreateTable(
                name: "ArticleSections",
                columns: table => new
                {
                    SectionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArticleID = table.Column<int>(type: "int", nullable: false),
                    SectionTitle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SectionContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ArticleS__80EF0892A9E4A151", x => x.SectionID);
                    table.ForeignKey(
                        name: "FK_ArticleSections_Articles",
                        column: x => x.ArticleID,
                        principalTable: "Articles",
                        principalColumn: "ArticleID");
                });

            migrationBuilder.CreateTable(
                name: "UserArticleProgress",
                columns: table => new
                {
                    ProgressID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    ArticleID = table.Column<int>(type: "int", nullable: false),
                    ProgressPercent = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LastAccessedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__UserArti__BAE29C85A832D5F7", x => x.ProgressID);
                    table.ForeignKey(
                        name: "FK_UserArticleProgress_Articles",
                        column: x => x.ArticleID,
                        principalTable: "Articles",
                        principalColumn: "ArticleID");
                    table.ForeignKey(
                        name: "FK_UserArticleProgress_Users",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    QuestionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartId = table.Column<int>(type: "int", nullable: false),
                    QuestionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StemText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PromptId = table.Column<int>(type: "int", nullable: true),
                    ScoreWeight = table.Column<int>(type: "int", nullable: false),
                    QuestionExplain = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Time = table.Column<int>(type: "int", nullable: false),
                    QuestionNumber = table.Column<int>(type: "int", nullable: false),
                    SampleAnswer = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Question__0DC06FACE795341E", x => x.QuestionId);
                    table.ForeignKey(
                        name: "FK_Questions_ExamParts",
                        column: x => x.PartId,
                        principalTable: "ExamParts",
                        principalColumn: "PartId");
                    table.ForeignKey(
                        name: "FK_Questions_Prompts",
                        column: x => x.PromptId,
                        principalTable: "Prompts",
                        principalColumn: "PromptId");
                });

            migrationBuilder.CreateTable(
                name: "SectionCompletions",
                columns: table => new
                {
                    CompletionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    SectionID = table.Column<int>(type: "int", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__SectionC__77FA70AF88803DC4", x => x.CompletionID);
                    table.ForeignKey(
                        name: "FK_SectionCompletions_ArticleSections",
                        column: x => x.SectionID,
                        principalTable: "ArticleSections",
                        principalColumn: "SectionID");
                    table.ForeignKey(
                        name: "FK_SectionCompletions_Users",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "UserNotes",
                columns: table => new
                {
                    NoteID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    ArticleID = table.Column<int>(type: "int", nullable: false),
                    SectionID = table.Column<int>(type: "int", nullable: false),
                    NoteContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreateAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UpdateAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__UserNote__EACE357FD9109DCF", x => x.NoteID);
                    table.ForeignKey(
                        name: "FK_UserNotes_ArticleSections",
                        column: x => x.SectionID,
                        principalTable: "ArticleSections",
                        principalColumn: "SectionID");
                    table.ForeignKey(
                        name: "FK_UserNotes_Articles",
                        column: x => x.ArticleID,
                        principalTable: "Articles",
                        principalColumn: "ArticleID");
                    table.ForeignKey(
                        name: "FK_UserNotes_Users",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "Options",
                columns: table => new
                {
                    OptionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Options__92C7A1FF9DEA13B7", x => x.OptionId);
                    table.ForeignKey(
                        name: "FK_Option_Questions",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "QuestionId");
                });

            migrationBuilder.CreateTable(
                name: "UserAnswers",
                columns: table => new
                {
                    UserAnswerID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttemptID = table.Column<int>(type: "int", nullable: false),
                    QuestionID = table.Column<int>(type: "int", nullable: false),
                    SelectedOptionID = table.Column<int>(type: "int", nullable: true),
                    AnswerContent = table.Column<string>(type: "text", nullable: true),
                    Score = table.Column<float>(type: "real", nullable: true),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: true),
                    FeedbackAI = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AudioURL = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__UserAnsw__47CE235F4AA17153", x => x.UserAnswerID);
                    table.ForeignKey(
                        name: "FK_UserAnswers_ExamAttempts",
                        column: x => x.AttemptID,
                        principalTable: "ExamAttempts",
                        principalColumn: "AttemptID");
                    table.ForeignKey(
                        name: "FK_UserAnswers_Options",
                        column: x => x.SelectedOptionID,
                        principalTable: "Options",
                        principalColumn: "OptionId");
                    table.ForeignKey(
                        name: "FK_UserAnswers_Questions",
                        column: x => x.QuestionID,
                        principalTable: "Questions",
                        principalColumn: "QuestionId");
                });

            migrationBuilder.CreateTable(
                name: "SpeakingResults",
                columns: table => new
                {
                    SpeakingResultId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PronunciationScore = table.Column<float>(type: "real", nullable: true),
                    AccuracyScore = table.Column<float>(type: "real", nullable: true),
                    FluencyScore = table.Column<float>(type: "real", nullable: true),
                    CompletenessScore = table.Column<float>(type: "real", nullable: true),
                    GrammarScore = table.Column<float>(type: "real", nullable: true),
                    VocabularyScore = table.Column<float>(type: "real", nullable: true),
                    ContentScore = table.Column<float>(type: "real", nullable: true),
                    UserAnswerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpeakingResults", x => x.SpeakingResultId);
                    table.ForeignKey(
                        name: "FK_SpeakingResults_UserAnswers_UserAnswerId",
                        column: x => x.UserAnswerId,
                        principalTable: "UserAnswers",
                        principalColumn: "UserAnswerID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_UserId",
                table: "Accounts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UQ__Accounts__536C85E4C923B0BE",
                table: "Accounts",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Accounts_AuthProvider_ProviderUserId",
                table: "Accounts",
                columns: new[] { "AuthProvider", "ProviderUserId" },
                unique: true,
                filter: "([AuthProvider] IS NOT NULL AND [ProviderUserId] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleCategories_CreatedByUserID",
                table: "ArticleCategories",
                column: "CreatedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_CategoryID",
                table: "Articles",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_CreatedBy",
                table: "Articles",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_UpdatedBy",
                table: "Articles",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleSections_ArticleID",
                table: "ArticleSections",
                column: "ArticleID");

            migrationBuilder.CreateIndex(
                name: "IX_Events_CreateBy",
                table: "Events",
                column: "CreateBy");

            migrationBuilder.CreateIndex(
                name: "IX_Events_UpdateBy",
                table: "Events",
                column: "UpdateBy");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttempts_ExamID",
                table: "ExamAttempts",
                column: "ExamID");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttempts_UserID",
                table: "ExamAttempts",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_ExamParts_ExamId",
                table: "ExamParts",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_CreatedBy",
                table: "Exams",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_UpdateBy",
                table: "Exams",
                column: "UpdateBy");

            migrationBuilder.CreateIndex(
                name: "IX_Options_QuestionId",
                table: "Options",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_UserId",
                table: "PasswordResetTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PackageID",
                table: "Payments",
                column: "PackageID");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_UserID",
                table: "Payments",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "UQ__Payments__0058A86E2C7EFF16",
                table: "Payments",
                column: "PaymentGatewayTransactionID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Prompts_PassageId",
                table: "Prompts",
                column: "PassageId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_PartId",
                table: "Questions",
                column: "PartId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_PromptId",
                table: "Questions",
                column: "PromptId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_SendBy",
                table: "Reports",
                column: "SendBy");

            migrationBuilder.CreateIndex(
                name: "IX_SectionCompletions_SectionID",
                table: "SectionCompletions",
                column: "SectionID");

            migrationBuilder.CreateIndex(
                name: "IX_SectionCompletions_UserID",
                table: "SectionCompletions",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Slides_CreateBy",
                table: "Slides",
                column: "CreateBy");

            migrationBuilder.CreateIndex(
                name: "IX_Slides_UpdateBy",
                table: "Slides",
                column: "UpdateBy");

            migrationBuilder.CreateIndex(
                name: "IX_SpeakingResults_UserAnswerId",
                table: "SpeakingResults",
                column: "UserAnswerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PackageID",
                table: "Subscriptions",
                column: "PackageID");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PaymentID",
                table: "Subscriptions",
                column: "PaymentID");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_UserID",
                table: "Subscriptions",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_UserAnswers_AttemptID",
                table: "UserAnswers",
                column: "AttemptID");

            migrationBuilder.CreateIndex(
                name: "IX_UserAnswers_QuestionID",
                table: "UserAnswers",
                column: "QuestionID");

            migrationBuilder.CreateIndex(
                name: "IX_UserAnswers_SelectedOptionID",
                table: "UserAnswers",
                column: "SelectedOptionID");

            migrationBuilder.CreateIndex(
                name: "IX_UserArticleProgress_ArticleID",
                table: "UserArticleProgress",
                column: "ArticleID");

            migrationBuilder.CreateIndex(
                name: "IX_UserArticleProgress_UserID",
                table: "UserArticleProgress",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_UserLeaderboard_LeaderboardID",
                table: "UserLeaderboard",
                column: "LeaderboardID");

            migrationBuilder.CreateIndex(
                name: "IX_UserLeaderboard_UserID",
                table: "UserLeaderboard",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotes_ArticleID",
                table: "UserNotes",
                column: "ArticleID");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotes_SectionID",
                table: "UserNotes",
                column: "SectionID");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotes_UserID",
                table: "UserNotes",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_NotificationId",
                table: "UserNotifications",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_UserId",
                table: "UserNotifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleID",
                table: "Users",
                column: "RoleID");

            migrationBuilder.CreateIndex(
                name: "UQ__Users__A9D105346CC2643F",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSpacedRepetition_UserId",
                table: "UserSpacedRepetition",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSpacedRepetition_VocabularyListId",
                table: "UserSpacedRepetition",
                column: "VocabularyListId");

            migrationBuilder.CreateIndex(
                name: "IX_Vocabularies_VocabularyListId",
                table: "Vocabularies",
                column: "VocabularyListId");

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyList_MakeBy",
                table: "VocabularyList",
                column: "MakeBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "PasswordResetTokens");

            migrationBuilder.DropTable(
                name: "Reports");

            migrationBuilder.DropTable(
                name: "SectionCompletions");

            migrationBuilder.DropTable(
                name: "Slides");

            migrationBuilder.DropTable(
                name: "SpeakingResults");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "UserArticleProgress");

            migrationBuilder.DropTable(
                name: "UserLeaderboard");

            migrationBuilder.DropTable(
                name: "UserNotes");

            migrationBuilder.DropTable(
                name: "UserNotifications");

            migrationBuilder.DropTable(
                name: "UserSpacedRepetition");

            migrationBuilder.DropTable(
                name: "Vocabularies");

            migrationBuilder.DropTable(
                name: "UserAnswers");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "Leaderboard");

            migrationBuilder.DropTable(
                name: "ArticleSections");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "VocabularyList");

            migrationBuilder.DropTable(
                name: "ExamAttempts");

            migrationBuilder.DropTable(
                name: "Options");

            migrationBuilder.DropTable(
                name: "Packages");

            migrationBuilder.DropTable(
                name: "Articles");

            migrationBuilder.DropTable(
                name: "Questions");

            migrationBuilder.DropTable(
                name: "ArticleCategories");

            migrationBuilder.DropTable(
                name: "ExamParts");

            migrationBuilder.DropTable(
                name: "Prompts");

            migrationBuilder.DropTable(
                name: "Exams");

            migrationBuilder.DropTable(
                name: "Passages");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
