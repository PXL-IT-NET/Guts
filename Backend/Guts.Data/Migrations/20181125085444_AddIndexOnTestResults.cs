using Microsoft.EntityFrameworkCore.Migrations;

namespace Guts.Data.Migrations
{
    public partial class AddIndexOnTestResults : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TestResults_Tests_TestId",
                table: "TestResults");

            migrationBuilder.DropIndex(
                name: "IX_TestResults_TestId",
                table: "TestResults");

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_TestId_UserId_CreateDateTime",
                table: "TestResults",
                columns: new[] { "TestId", "UserId", "CreateDateTime" });

            migrationBuilder.AddForeignKey(
                name: "FK_TestResults_Tests_TestId",
                table: "TestResults",
                column: "TestId",
                principalTable: "Tests",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TestResults_Tests_TestId",
                table: "TestResults"); 

            migrationBuilder.DropIndex(
                name: "IX_TestResults_TestId_UserId_CreateDateTime",
                table: "TestResults");

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_TestId",
                table: "TestResults",
                column: "TestId");

            migrationBuilder.AddForeignKey(
                name: "FK_TestResults_Tests_TestId", 
                table: "TestResults", 
                column: "TestId",
                principalTable: "Tests", 
                principalColumn: "Id");
        }
    }
}
