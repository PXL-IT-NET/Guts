using Microsoft.EntityFrameworkCore.Migrations;

namespace Guts.Infrastructure.Migrations
{
    public partial class RemoveSourceCodeFromTestRun : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceCode",
                table: "TestRuns");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceCode",
                table: "TestRuns",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
