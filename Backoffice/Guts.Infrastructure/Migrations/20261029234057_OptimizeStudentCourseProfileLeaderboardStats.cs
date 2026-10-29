using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Guts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeStudentCourseProfileLeaderboardStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StudentCourseProfiles_CourseId",
                table: "StudentCourseProfiles");

            migrationBuilder.AddColumn<int>(
                name: "BestHallOfFameRank",
                table: "StudentCourseProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BestHallOfFameTier",
                table: "StudentCourseProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastXpEarnedAt",
                table: "StudentCourseProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalGreenTests",
                table: "StudentCourseProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_StudentCourseProfiles_CourseId_PeriodId_ShowInLeaderBoard",
                table: "StudentCourseProfiles",
                columns: new[] { "CourseId", "PeriodId", "ShowInLeaderBoard" });

            migrationBuilder.Sql(@"
                UPDATE [StudentCourseProfiles]
                SET
                    [TotalGreenTests] = COALESCE((
                        SELECT COUNT(1)
                        FROM [GamificationEvents] ge
                        WHERE ge.[StudentCourseProfileId] = [StudentCourseProfiles].[Id]
                          AND ge.[EventType] = 1
                    ), 0),
                    [LastXpEarnedAt] = (
                        SELECT MAX(ge.[CreatedAt])
                        FROM [GamificationEvents] ge
                        WHERE ge.[StudentCourseProfileId] = [StudentCourseProfiles].[Id]
                          AND ge.[XpEarned] > 0
                    ),
                    [BestHallOfFameRank] = (
                        SELECT MIN(ge.[TargetId])
                        FROM [GamificationEvents] ge
                        WHERE ge.[StudentCourseProfileId] = [StudentCourseProfiles].[Id]
                          AND ge.[EventType] = 6
                    ),
                    [BestHallOfFameTier] = (
                        SELECT MAX(ge.[TargetId])
                        FROM [GamificationEvents] ge
                        WHERE ge.[StudentCourseProfileId] = [StudentCourseProfiles].[Id]
                          AND ge.[EventType] = 7
                    );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StudentCourseProfiles_CourseId_PeriodId_ShowInLeaderBoard",
                table: "StudentCourseProfiles");

            migrationBuilder.DropColumn(
                name: "BestHallOfFameRank",
                table: "StudentCourseProfiles");

            migrationBuilder.DropColumn(
                name: "BestHallOfFameTier",
                table: "StudentCourseProfiles");

            migrationBuilder.DropColumn(
                name: "LastXpEarnedAt",
                table: "StudentCourseProfiles");

            migrationBuilder.DropColumn(
                name: "TotalGreenTests",
                table: "StudentCourseProfiles");

            migrationBuilder.CreateIndex(
                name: "IX_StudentCourseProfiles_CourseId",
                table: "StudentCourseProfiles",
                column: "CourseId");
        }
    }
}
