using Microsoft.EntityFrameworkCore.Migrations;

namespace Guts.Infrastructure.Migrations
{
    public partial class FixAssignmentEvaluationRelation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AssignmentEvaluation_AssignmentId",
                table: "AssignmentEvaluation");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentEvaluation_AssignmentId",
                table: "AssignmentEvaluation",
                column: "AssignmentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AssignmentEvaluation_AssignmentId",
                table: "AssignmentEvaluation");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentEvaluation_AssignmentId",
                table: "AssignmentEvaluation",
                column: "AssignmentId",
                unique: true);
        }
    }
}
