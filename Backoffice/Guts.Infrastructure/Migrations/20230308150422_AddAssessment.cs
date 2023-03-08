using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Guts.Infrastructure.Migrations
{
    public partial class AddAssessment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectAssessments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OpenOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeadlineUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectAssessments_Topics_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Topics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectTeamAssessments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectAssessmentId = table.Column<int>(type: "int", nullable: false),
                    ProjectTeamId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTeamAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectTeamAssessments_ProjectAssessments_ProjectAssessmentId",
                        column: x => x.ProjectAssessmentId,
                        principalTable: "ProjectAssessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectTeamAssessments_ProjectTeams_ProjectTeamId",
                        column: x => x.ProjectTeamId,
                        principalTable: "ProjectTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PeerAssessments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    CooperationScore = table.Column<int>(type: "int", nullable: false),
                    ContributionScore = table.Column<int>(type: "int", nullable: false),
                    EffortScore = table.Column<int>(type: "int", nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProjectTeamAssessmentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeerAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PeerAssessments_ProjectTeamAssessments_ProjectTeamAssessmentId",
                        column: x => x.ProjectTeamAssessmentId,
                        principalTable: "ProjectTeamAssessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PeerAssessments_Users_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PeerAssessments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PeerAssessments_ProjectTeamAssessmentId",
                table: "PeerAssessments",
                column: "ProjectTeamAssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PeerAssessments_SubjectId",
                table: "PeerAssessments",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_PeerAssessments_UserId",
                table: "PeerAssessments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectAssessments_ProjectId",
                table: "ProjectAssessments",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTeamAssessments_ProjectAssessmentId",
                table: "ProjectTeamAssessments",
                column: "ProjectAssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTeamAssessments_ProjectTeamId",
                table: "ProjectTeamAssessments",
                column: "ProjectTeamId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PeerAssessments");

            migrationBuilder.DropTable(
                name: "ProjectTeamAssessments");

            migrationBuilder.DropTable(
                name: "ProjectAssessments");
        }
    }
}
