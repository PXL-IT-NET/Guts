using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Guts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveExamAggregate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssignmentEvaluation");

            migrationBuilder.DropTable(
                name: "ExamParts");

            migrationBuilder.DropTable(
                name: "Exams");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Exams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    PeriodId = table.Column<int>(type: "int", nullable: false),
                    MaximumScore = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Exams_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Exams_Periods_PeriodId",
                        column: x => x.PeriodId,
                        principalTable: "Periods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExamParts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Deadline = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamParts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamParts_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssignmentEvaluation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssignmentId = table.Column<int>(type: "int", nullable: false),
                    ExamPartId = table.Column<int>(type: "int", nullable: false),
                    MaximumScore = table.Column<int>(type: "int", nullable: false),
                    NumberOfTestsAlreadyGreenAtStart = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentEvaluation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssignmentEvaluation_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssignmentEvaluation_ExamParts_ExamPartId",
                        column: x => x.ExamPartId,
                        principalTable: "ExamParts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentEvaluation_AssignmentId",
                table: "AssignmentEvaluation",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentEvaluation_ExamPartId",
                table: "AssignmentEvaluation",
                column: "ExamPartId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamParts_ExamId",
                table: "ExamParts",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_CourseId",
                table: "Exams",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_PeriodId",
                table: "Exams",
                column: "PeriodId");
        }
    }
}
