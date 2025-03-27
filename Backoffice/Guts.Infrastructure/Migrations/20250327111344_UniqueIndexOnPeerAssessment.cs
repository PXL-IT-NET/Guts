using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Guts.Infrastructure.Migrations
{
    public partial class UniqueIndexOnPeerAssessment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PeerAssessments_ProjectTeamAssessmentId",
                table: "PeerAssessments");

            migrationBuilder.CreateIndex(
                name: "IX_PeerAssessments_ProjectTeamAssessmentId_UserId_SubjectId",
                table: "PeerAssessments",
                columns: new[] { "ProjectTeamAssessmentId", "UserId", "SubjectId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PeerAssessments_ProjectTeamAssessmentId_UserId_SubjectId",
                table: "PeerAssessments");

            migrationBuilder.CreateIndex(
                name: "IX_PeerAssessments_ProjectTeamAssessmentId",
                table: "PeerAssessments",
                column: "ProjectTeamAssessmentId");
        }
    }
}
