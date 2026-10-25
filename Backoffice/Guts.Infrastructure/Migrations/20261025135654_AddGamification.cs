using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Guts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGamification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CourseClassGroups",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CourseId = table.Column<int>(nullable: false),
                    PeriodId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseClassGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseClassGroups_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseClassGroups_Periods_PeriodId",
                        column: x => x.PeriodId,
                        principalTable: "Periods",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CourseGamificationConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(nullable: false),
                    IsGamificationEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsWeeklyStreakEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IsLeaderboardEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IsClassCompetitionEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IsTeamSynergyEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IsBadgesEnabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseGamificationConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseGamificationConfigs_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentCourseProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(nullable: false),
                    CourseId = table.Column<int>(nullable: false),
                    PeriodId = table.Column<int>(nullable: false),
                    ShowInLeaderBoard = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    UsePseudonym = table.Column<bool>(type: "bit", nullable: false),
                    Pseudonym = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Avatar = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ClassGroupId = table.Column<int>(type: "int", nullable: true),
                    TotalXp = table.Column<int>(type: "int", nullable: false),
                    CurrentWeeklyStreak = table.Column<int>(type: "int", nullable: true),
                    BestWeeklyStreak = table.Column<int>(type: "int", nullable: true),
                    StreakLastActiveYear = table.Column<int>(type: "int", nullable: true),
                    StreakLastActiveWeek = table.Column<int>(type: "int", nullable: true),
                    LastLeaderboardVisitDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentCourseProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentCourseProfiles_CourseClassGroups_ClassGroupId",
                        column: x => x.ClassGroupId,
                        principalTable: "CourseClassGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StudentCourseProfiles_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudentCourseProfiles_Periods_PeriodId",
                        column: x => x.PeriodId,
                        principalTable: "Periods",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudentCourseProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GamificationEvents",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentCourseProfileId = table.Column<int>(nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    TargetId = table.Column<int>(nullable: false),
                    XpEarned = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GamificationEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GamificationEvents_StudentCourseProfiles_StudentCourseProfileId",
                        column: x => x.StudentCourseProfileId,
                        principalTable: "StudentCourseProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentBadges",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentCourseProfileId = table.Column<int>(nullable: false),
                    BadgeType = table.Column<int>(type: "int", nullable: false),
                    EarnedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentBadges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentBadges_StudentCourseProfiles_StudentCourseProfileId",
                        column: x => x.StudentCourseProfileId,
                        principalTable: "StudentCourseProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseClassGroups_CourseId_PeriodId_Name",
                table: "CourseClassGroups",
                columns: new[] { "CourseId", "PeriodId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseClassGroups_PeriodId",
                table: "CourseClassGroups",
                column: "PeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseGamificationConfigs_CourseId",
                table: "CourseGamificationConfigs",
                column: "CourseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GamificationEvents_StudentCourseProfileId_EventType_TargetId",
                table: "GamificationEvents",
                columns: new[] { "StudentCourseProfileId", "EventType", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentBadges_StudentCourseProfileId_BadgeType",
                table: "StudentBadges",
                columns: new[] { "StudentCourseProfileId", "BadgeType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentCourseProfiles_ClassGroupId",
                table: "StudentCourseProfiles",
                column: "ClassGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentCourseProfiles_CourseId",
                table: "StudentCourseProfiles",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentCourseProfiles_PeriodId",
                table: "StudentCourseProfiles",
                column: "PeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentCourseProfiles_UserId_CourseId_PeriodId",
                table: "StudentCourseProfiles",
                columns: new[] { "UserId", "CourseId", "PeriodId" },
                unique: true);

            // Seed default gamification configs for existing courses (IsGamificationEnabled = 0 by default)
            migrationBuilder.Sql(@"
                INSERT INTO [CourseGamificationConfigs] (
                    [CourseId],
                    [IsGamificationEnabled],
                    [IsWeeklyStreakEnabled],
                    [IsLeaderboardEnabled],
                    [IsClassCompetitionEnabled],
                    [IsTeamSynergyEnabled],
                    [IsBadgesEnabled]
                )
                SELECT c.[Id], 0, 1, 1, 1, 1, 1
                FROM [Courses] c
                WHERE NOT EXISTS (
                    SELECT 1 FROM [CourseGamificationConfigs] cfg WHERE cfg.[CourseId] = c.[Id]
                );
            ");

            // Scan historical test runs and create StudentCourseProfiles + GamificationEvents (ShowInLeaderBoard = 1 by default)
            migrationBuilder.Sql(@"
                ;WITH DistinctPassedTests AS (
                    SELECT
                        tr.[UserId],
                        t.[CourseId],
                        t.[PeriodId],
                        tr.[TestId],
                        MIN(tr.[CreateDateTime]) AS FirstPassedAt
                    FROM [TestResults] tr
                    INNER JOIN [TestRuns] run ON tr.[TestRunId] = run.[Id]
                    INNER JOIN [Assignments] a ON run.[AssignmentId] = a.[Id]
                    INNER JOIN [Topics] t ON a.[TopicId] = t.[Id]
                    WHERE tr.[Passed] = 1
                      AND NOT EXISTS (
                          SELECT 1 FROM [AssignmentEvaluation] ae WHERE ae.[AssignmentId] = a.[Id]
                      )
                    GROUP BY tr.[UserId], t.[CourseId], t.[PeriodId], tr.[TestId]
                )
                INSERT INTO [StudentCourseProfiles] (
                    [UserId],
                    [CourseId],
                    [PeriodId],
                    [ShowInLeaderBoard],
                    [UsePseudonym],
                    [Pseudonym],
                    [Avatar],
                    [TotalXp],
                    [CurrentWeeklyStreak],
                    [BestWeeklyStreak],
                    [StreakLastActiveYear],
                    [StreakLastActiveWeek]
                )
                SELECT
                    dp.[UserId],
                    dp.[CourseId],
                    dp.[PeriodId],
                    1,
                    1,
                    CONCAT('Student_', dp.[UserId]),
                    'CodeCat',
                    0,
                    0,
                    0,
                    0,
                    0
                FROM (SELECT DISTINCT [UserId], [CourseId], [PeriodId] FROM DistinctPassedTests) dp
                WHERE NOT EXISTS (
                    SELECT 1 FROM [StudentCourseProfiles] sp
                    WHERE sp.[UserId] = dp.[UserId]
                      AND sp.[CourseId] = dp.[CourseId]
                      AND sp.[PeriodId] = dp.[PeriodId]
                );

                ;WITH DistinctPassedTests AS (
                    SELECT
                        tr.[UserId],
                        t.[CourseId],
                        t.[PeriodId],
                        tr.[TestId],
                        MIN(tr.[CreateDateTime]) AS FirstPassedAt
                    FROM [TestResults] tr
                    INNER JOIN [TestRuns] run ON tr.[TestRunId] = run.[Id]
                    INNER JOIN [Assignments] a ON run.[AssignmentId] = a.[Id]
                    INNER JOIN [Topics] t ON a.[TopicId] = t.[Id]
                    WHERE tr.[Passed] = 1
                      AND NOT EXISTS (
                          SELECT 1 FROM [AssignmentEvaluation] ae WHERE ae.[AssignmentId] = a.[Id]
                      )
                    GROUP BY tr.[UserId], t.[CourseId], t.[PeriodId], tr.[TestId]
                )
                INSERT INTO [GamificationEvents] (
                    [StudentCourseProfileId],
                    [EventType],
                    [TargetId],
                    [XpEarned],
                    [CreatedAt]
                )
                SELECT
                    sp.[Id],
                    1, -- PassedTest
                    dp.[TestId],
                    10, -- 10 XP
                    dp.[FirstPassedAt]
                FROM DistinctPassedTests dp
                INNER JOIN [StudentCourseProfiles] sp
                    ON sp.[UserId] = dp.[UserId]
                   AND sp.[CourseId] = dp.[CourseId]
                   AND sp.[PeriodId] = dp.[PeriodId]
                WHERE NOT EXISTS (
                    SELECT 1 FROM [GamificationEvents] ge
                    WHERE ge.[StudentCourseProfileId] = sp.[Id]
                      AND ge.[EventType] = 1
                      AND ge.[TargetId] = dp.[TestId]
                );

                -- Update TotalXp on StudentCourseProfiles
                UPDATE [StudentCourseProfiles]
                SET [TotalXp] = COALESCE((
                    SELECT SUM([XpEarned])
                    FROM [GamificationEvents]
                    WHERE [GamificationEvents].[StudentCourseProfileId] = [StudentCourseProfiles].[Id]
                ), 0);

                -- Award FirstSpark badge (1) to all profiles with at least 1 passing test
                INSERT INTO [StudentBadges] (
                    [StudentCourseProfileId],
                    [BadgeType],
                    [EarnedAt]
                )
                SELECT
                    sp.[Id],
                    1,
                    MIN(ge.[CreatedAt])
                FROM [StudentCourseProfiles] sp
                INNER JOIN [GamificationEvents] ge ON ge.[StudentCourseProfileId] = sp.[Id] AND ge.[EventType] = 1
                WHERE NOT EXISTS (
                    SELECT 1 FROM [StudentBadges] sb
                    WHERE sb.[StudentCourseProfileId] = sp.[Id] AND sb.[BadgeType] = 1
                )
                GROUP BY sp.[Id];

                -- Award CenturyClub badge (8) to profiles with at least 100 passing tests
                INSERT INTO [StudentBadges] (
                    [StudentCourseProfileId],
                    [BadgeType],
                    [EarnedAt]
                )
                SELECT
                    sp.[Id],
                    8,
                    MAX(ge.[CreatedAt])
                FROM [StudentCourseProfiles] sp
                INNER JOIN [GamificationEvents] ge ON ge.[StudentCourseProfileId] = sp.[Id] AND ge.[EventType] = 1
                WHERE NOT EXISTS (
                    SELECT 1 FROM [StudentBadges] sb
                    WHERE sb.[StudentCourseProfileId] = sp.[Id] AND sb.[BadgeType] = 8
                )
                GROUP BY sp.[Id]
                HAVING COUNT(ge.[Id]) >= 100;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseGamificationConfigs");

            migrationBuilder.DropTable(
                name: "GamificationEvents");

            migrationBuilder.DropTable(
                name: "StudentBadges");

            migrationBuilder.DropTable(
                name: "StudentCourseProfiles");

            migrationBuilder.DropTable(
                name: "CourseClassGroups");
        }
    }
}
