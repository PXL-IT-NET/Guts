using Microsoft.EntityFrameworkCore.Migrations;

namespace Guts.Data.Migrations
{
    public partial class AddSourceCodeToTestRun : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceCode",
                table: "TestRuns",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceCode",
                table: "TestRuns");
        }
    }
}
