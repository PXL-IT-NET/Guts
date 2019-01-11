using System.Text;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Guts.Data.Migrations
{
    public partial class EmbraceAssignments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_Chapters_ChapterId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_Projects_ProjectId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Courses_CourseId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Periods_PeriodId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectTeams_Projects_ProjectId",
                table: "ProjectTeams");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_ChapterId",
                table: "Assignments");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_ProjectId",
                table: "Assignments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Projects",
                table: "Projects");

            //danger zone

            migrationBuilder.RenameTable(
                name: "Projects",
                newName: "Topics");

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "Topics",
                nullable: true);

            // set Discriminator to "Project" in Topics table
            migrationBuilder.Sql("update Topics " +
                                 "set Discriminator = 'Project';");

            migrationBuilder.AlterColumn<string>(
                name: "Discriminator",
                table: "Topics",
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "OldChapterId",
                table: "Topics",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Topic",
                table: "Topics",
                column: "Id");

            migrationBuilder.Sql("insert into Topics(Code, Description, CourseId, PeriodId, Discriminator, OldChapterId) " +
                                 "select Number, CONCAT('Chapter ',Number), CourseId, PeriodId, 'Chapter', Id from Chapters;");

            migrationBuilder.AddColumn<int>(
                name: "TopicId",
                table: "Assignments",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("update Assignments a inner join Topics t on a.ChapterId = t.OldChapterId " +
                                 "set TopicId = t.Id " +
                                 "where a.Discriminator = 'Exercise';");

            migrationBuilder.Sql("update Assignments " +
                                 "set TopicId = ProjectId " +
                                 "where Discriminator = 'ProjectComponent';");

            migrationBuilder.DropColumn(
                name: "OldChapterId",
                table: "Topics");

            migrationBuilder.DropTable(
                name: "Chapters");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "ChapterId",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Assignments");

            migrationBuilder.RenameIndex(
                name: "IX_Projects_PeriodId",
                table: "Topics",
                newName: "IX_Topic_PeriodId");

            migrationBuilder.RenameIndex(
                name: "IX_Projects_CourseId",
                table: "Topics",
                newName: "IX_Topic_CourseId");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "ProjectTeams",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_TopicId",
                table: "Assignments",
                column: "TopicId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_Topic_TopicId",
                table: "Assignments",
                column: "TopicId",
                principalTable: "Topics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTeams_Topic_ProjectId",
                table: "ProjectTeams",
                column: "ProjectId",
                principalTable: "Topics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Topic_Courses_CourseId",
                table: "Topics",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Topic_Periods_PeriodId",
                table: "Topics",
                column: "PeriodId",
                principalTable: "Periods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.Sql("DROP PROCEDURE sp_getUsersOfChapter;");

            var sb = new StringBuilder();
            sb.AppendLine("CREATE PROCEDURE sp_getUsersOfTopic(IN TopicId INT) ");
            sb.AppendLine("LANGUAGE SQL");
            sb.AppendLine("NOT DETERMINISTIC");
            sb.AppendLine("CONTAINS SQL");
            sb.AppendLine("SQL SECURITY DEFINER");
            sb.AppendLine("COMMENT ''");
            sb.AppendLine("BEGIN");
            sb.AppendLine("select u.*");
            sb.AppendLine("from (");
            sb.AppendLine(" select u.Id");
            sb.AppendLine(" from Users u");
            sb.AppendLine(" inner join TestRuns tr on tr.UserId = u.Id");
            sb.AppendLine(" inner join Assignments a on tr.AssignmentId = a.Id");
            sb.AppendLine(" where a.TopicId = TopicId");
            sb.AppendLine(" group by u.Id) as g");
            sb.AppendLine("inner join Users u on u.Id = g.Id");
            sb.AppendLine("order by u.FirstName, u.LastName;");
            sb.AppendLine(@"END");
            migrationBuilder.Sql(sb.ToString());
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_Topic_TopicId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectTeams_Topic_ProjectId",
                table: "ProjectTeams");

            migrationBuilder.DropForeignKey(
                name: "FK_Topic_Courses_CourseId",
                table: "Topics");

            migrationBuilder.DropForeignKey(
                name: "FK_Topic_Periods_PeriodId",
                table: "Topics");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_TopicId",
                table: "Assignments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Topic",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "ProjectTeams");

            migrationBuilder.DropColumn(
                name: "TopicId",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "Topics");

            migrationBuilder.RenameTable(
                name: "Topics",
                newName: "Projects");

            migrationBuilder.RenameIndex(
                name: "IX_Topic_PeriodId",
                table: "Projects",
                newName: "IX_Projects_PeriodId");

            migrationBuilder.RenameIndex(
                name: "IX_Topic_CourseId",
                table: "Projects",
                newName: "IX_Projects_CourseId");

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "Assignments",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ChapterId",
                table: "Assignments",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "Assignments",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Projects",
                table: "Projects",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Chapters",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CourseId = table.Column<int>(nullable: false),
                    Number = table.Column<int>(nullable: false),
                    PeriodId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chapters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Chapters_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Chapters_Periods_PeriodId",
                        column: x => x.PeriodId,
                        principalTable: "Periods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_ChapterId",
                table: "Assignments",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_ProjectId",
                table: "Assignments",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_CourseId",
                table: "Chapters",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_PeriodId",
                table: "Chapters",
                column: "PeriodId");

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
                name: "FK_Projects_Courses_CourseId",
                table: "Projects",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Periods_PeriodId",
                table: "Projects",
                column: "PeriodId",
                principalTable: "Periods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTeams_Projects_ProjectId",
                table: "ProjectTeams",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.Sql("DROP PROCEDURE sp_getUsersOfTopic;");

            var sb = new StringBuilder();
            sb.AppendLine("CREATE PROCEDURE sp_getUsersOfChapter(IN ChapterId INT) ");
            sb.AppendLine("LANGUAGE SQL");
            sb.AppendLine("NOT DETERMINISTIC");
            sb.AppendLine("CONTAINS SQL");
            sb.AppendLine("SQL SECURITY DEFINER");
            sb.AppendLine("COMMENT ''");
            sb.AppendLine("BEGIN");
            sb.AppendLine("select u.*");
            sb.AppendLine("from (");
            sb.AppendLine(" select u.Id");
            sb.AppendLine(" from Users u");
            sb.AppendLine(" inner join TestRuns tr on tr.UserId = u.Id");
            sb.AppendLine(" inner join Assignments a on tr.AssignmentId = a.Id");
            sb.AppendLine(" where a.ChapterId = ChapterId");
            sb.AppendLine(" group by u.Id) as g");
            sb.AppendLine("inner join Users u on u.Id = g.Id");
            sb.AppendLine("order by u.FirstName, u.LastName;");
            sb.AppendLine(@"END");
            migrationBuilder.Sql(sb.ToString());
        }
    }
}
