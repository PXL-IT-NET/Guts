using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Guts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CascadeDeleteStudentCourseProfilesOnPeriodAndCourse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseClassGroups_Courses_CourseId",
                table: "CourseClassGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentCourseProfiles_Courses_CourseId",
                table: "StudentCourseProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentCourseProfiles_Periods_PeriodId",
                table: "StudentCourseProfiles");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseClassGroups_Courses_CourseId",
                table: "CourseClassGroups",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentCourseProfiles_Courses_CourseId",
                table: "StudentCourseProfiles",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentCourseProfiles_Periods_PeriodId",
                table: "StudentCourseProfiles",
                column: "PeriodId",
                principalTable: "Periods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseClassGroups_Courses_CourseId",
                table: "CourseClassGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentCourseProfiles_Courses_CourseId",
                table: "StudentCourseProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentCourseProfiles_Periods_PeriodId",
                table: "StudentCourseProfiles");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseClassGroups_Courses_CourseId",
                table: "CourseClassGroups",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentCourseProfiles_Courses_CourseId",
                table: "StudentCourseProfiles",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentCourseProfiles_Periods_PeriodId",
                table: "StudentCourseProfiles",
                column: "PeriodId",
                principalTable: "Periods",
                principalColumn: "Id");
        }
    }
}
