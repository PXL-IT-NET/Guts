using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Guts.Data.Migrations
{
    public partial class AddProject : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Exercises_Chapters_ChapterId",
                table: "Exercises");

            migrationBuilder.DropForeignKey(
                name: "FK_TestRuns_Exercises_ExerciseId",
                table: "TestRuns");

            migrationBuilder.DropForeignKey(
                name: "FK_Tests_Exercises_ExerciseId",
                table: "Tests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Exercises",
                table: "Exercises");

            //migrationBuilder.DropColumn(
            //    name: "Number",
            //    table: "Exercises");

            migrationBuilder.RenameTable(
                name: "Exercises",
                newName: "Assignments");

            migrationBuilder.RenameColumn(
                name: "ExerciseId",
                table: "Tests",
                newName: "AssignmentId");

            migrationBuilder.RenameIndex(
                name: "IX_Tests_ExerciseId",
                table: "Tests",
                newName: "IX_Tests_AssignmentId");

            migrationBuilder.RenameColumn(
                name: "ExerciseId",
                table: "TestRuns",
                newName: "AssignmentId");

            migrationBuilder.RenameIndex(
                name: "IX_TestRuns_ExerciseId",
                table: "TestRuns",
                newName: "IX_TestRuns_AssignmentId");

            migrationBuilder.RenameIndex(
                name: "IX_Exercises_ChapterId",
                table: "Assignments",
                newName: "IX_Assignments_ChapterId");

            migrationBuilder.AlterColumn<int>(
                name: "ChapterId",
                table: "Assignments",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Assignments",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            //copy Number to Code
            migrationBuilder.Sql("update Assignments " +
                                 "set Code = CONVERT(Number, CHAR);");


            migrationBuilder.DropColumn(
                name: "Number",
                table: "Assignments");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Assignments",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "Assignments",
                nullable: true);

            migrationBuilder.Sql("update Assignments " +
                                 "set Discriminator = 'Exercise';");

            migrationBuilder.AlterColumn<string>(
                name: "Discriminator",
                table: "Assignments",
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "Assignments",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Assignments",
                table: "Assignments",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(maxLength: 20, nullable: false),
                    Description = table.Column<string>(nullable: false),
                    CourseId = table.Column<int>(nullable: false),
                    PeriodId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projects_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Projects_Periods_PeriodId",
                        column: x => x.PeriodId,
                        principalTable: "Periods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectTeams",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProjectId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTeams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectTeams_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectTeamUsers",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProjectTeamId = table.Column<int>(nullable: false),
                    UserId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTeamUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectTeamUsers_ProjectTeams_ProjectTeamId",
                        column: x => x.ProjectTeamId,
                        principalTable: "ProjectTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectTeamUsers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_ProjectId",
                table: "Assignments",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CourseId",
                table: "Projects",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_PeriodId",
                table: "Projects",
                column: "PeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTeams_ProjectId",
                table: "ProjectTeams",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTeamUsers_ProjectTeamId",
                table: "ProjectTeamUsers",
                column: "ProjectTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTeamUsers_UserId",
                table: "ProjectTeamUsers",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_Chapters_ChapterId",
                table: "Assignments",
                column: "ChapterId",
                principalTable: "Chapters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_Projects_ProjectId",
                table: "Assignments",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TestRuns_Assignments_AssignmentId",
                table: "TestRuns",
                column: "AssignmentId",
                principalTable: "Assignments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tests_Assignments_AssignmentId",
                table: "Tests",
                column: "AssignmentId",
                principalTable: "Assignments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_Chapters_ChapterId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_Projects_ProjectId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_TestRuns_Assignments_AssignmentId",
                table: "TestRuns");

            migrationBuilder.DropForeignKey(
                name: "FK_Tests_Assignments_AssignmentId",
                table: "Tests");

            migrationBuilder.DropTable(
                name: "ProjectTeamUsers");

            migrationBuilder.DropTable(
                name: "ProjectTeams");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Assignments",
                table: "Assignments");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_ProjectId",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Assignments");

            migrationBuilder.RenameTable(
                name: "Assignments",
                newName: "Exercises");

            migrationBuilder.RenameColumn(
                name: "AssignmentId",
                table: "Tests",
                newName: "ExerciseId");

            migrationBuilder.RenameIndex(
                name: "IX_Tests_AssignmentId",
                table: "Tests",
                newName: "IX_Tests_ExerciseId");

            migrationBuilder.RenameColumn(
                name: "AssignmentId",
                table: "TestRuns",
                newName: "ExerciseId");

            migrationBuilder.RenameIndex(
                name: "IX_TestRuns_AssignmentId",
                table: "TestRuns",
                newName: "IX_TestRuns_ExerciseId");

            migrationBuilder.RenameIndex(
                name: "IX_Assignments_ChapterId",
                table: "Exercises",
                newName: "IX_Exercises_ChapterId");

            migrationBuilder.AlterColumn<int>(
                name: "ChapterId",
                table: "Exercises",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Number",
                table: "Exercises",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Exercises",
                table: "Exercises",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Exercises_Chapters_ChapterId",
                table: "Exercises",
                column: "ChapterId",
                principalTable: "Chapters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TestRuns_Exercises_ExerciseId",
                table: "TestRuns",
                column: "ExerciseId",
                principalTable: "Exercises",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tests_Exercises_ExerciseId",
                table: "Tests",
                column: "ExerciseId",
                principalTable: "Exercises",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
