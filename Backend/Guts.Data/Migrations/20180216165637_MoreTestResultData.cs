using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Guts.Data.Migrations
{
    public partial class MoreTestResultData : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UserNameIndex",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "RoleNameIndex",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "Index",
                table: "TestRuns");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateDateTime",
                table: "TestResults",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "TestResults",
                nullable: false,
                defaultValue: 0);

            //copy users and dates from TestRun to TestResults
            migrationBuilder.Sql("update TestResults " +
                                 "inner join TestRuns on TestRuns.Id = TestResults.TestRunId " +
                                 "set TestResults.CreateDateTime = TestRuns.CreateDateTime, TestResults.UserId = TestRuns.UserId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "Users",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_UserId",
                table: "TestResults",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "Roles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TestResults_Users_UserId",
                table: "TestResults",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TestResults_Users_UserId",
                table: "TestResults");

            migrationBuilder.DropIndex(
                name: "UserNameIndex",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_TestResults_UserId",
                table: "TestResults");

            migrationBuilder.DropIndex(
                name: "RoleNameIndex",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "CreateDateTime",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "TestResults");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "Users",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "Roles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");
        }
    }
}
