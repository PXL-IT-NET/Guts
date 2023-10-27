using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Guts.Infrastructure.Migrations
{
    public partial class AddIsDraftToPeerAssessment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDraft",
                table: "PeerAssessments",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDraft",
                table: "PeerAssessments");
        }
    }
}
