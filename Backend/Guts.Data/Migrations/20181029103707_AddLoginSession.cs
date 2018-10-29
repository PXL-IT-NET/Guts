using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Guts.Data.Migrations
{
    public partial class AddLoginSession : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LoginSessions",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PublicIdentifier = table.Column<string>(nullable: false),
                    SessionToken = table.Column<string>(nullable: false),
                    IpAddress = table.Column<string>(nullable: false),
                    LoginToken = table.Column<string>(nullable: true),
                    IsCancelled = table.Column<bool>(nullable: false),
                    CreateDateTime = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginSessions", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoginSessions");
        }
    }
}
