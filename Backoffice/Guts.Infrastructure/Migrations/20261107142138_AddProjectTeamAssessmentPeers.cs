using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Guts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectTeamAssessmentPeers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectTeamAssessmentPeers",
                columns: table => new
                {
                    ProjectTeamAssessmentId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTeamAssessmentPeers", x => new { x.ProjectTeamAssessmentId, x.UserId });
                    table.ForeignKey(
                        name: "FK_ProjectTeamAssessmentPeers_ProjectTeamAssessments_ProjectTeamAssessmentId",
                        column: x => x.ProjectTeamAssessmentId,
                        principalTable: "ProjectTeamAssessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectTeamAssessmentPeers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTeamAssessmentPeers_UserId",
                table: "ProjectTeamAssessmentPeers",
                column: "UserId");

            migrationBuilder.Sql(@"
                INSERT INTO ProjectTeamAssessmentPeers (ProjectTeamAssessmentId, UserId)
                SELECT DISTINCT candidates.ProjectTeamAssessmentId, candidates.UserId
                FROM (
                    SELECT pta.Id AS ProjectTeamAssessmentId, ptu.UserId AS UserId
                    FROM ProjectTeamAssessments pta
                    INNER JOIN ProjectTeamUsers ptu ON ptu.ProjectTeamId = pta.ProjectTeamId
                    UNION
                    SELECT pa.ProjectTeamAssessmentId AS ProjectTeamAssessmentId, pa.UserId AS UserId
                    FROM PeerAssessments pa
                    UNION
                    SELECT pa.ProjectTeamAssessmentId AS ProjectTeamAssessmentId, pa.SubjectId AS UserId
                    FROM PeerAssessments pa
                ) candidates
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM ProjectTeamAssessmentPeers existing
                    WHERE existing.ProjectTeamAssessmentId = candidates.ProjectTeamAssessmentId
                      AND existing.UserId = candidates.UserId
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectTeamAssessmentPeers");
        }
    }
}
