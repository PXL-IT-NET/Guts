using System;
using System.IO;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Guts.Infrastructure.Migrations
{
    public partial class AddSolutionFiles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SolutionFiles",
                columns: table => new
                {
                    AssignmentId = table.Column<int>(nullable: false),
                    UserId = table.Column<int>(nullable: false),
                    ModifyDateTime = table.Column<DateTime>(nullable: false),
                    FilePath = table.Column<string>(maxLength: 255, nullable: false),
                    Content = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolutionFiles", x => new { x.AssignmentId, x.UserId, x.ModifyDateTime, x.FilePath });
                    table.ForeignKey(
                        name: "FK_SolutionFiles_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SolutionFiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SolutionFiles_UserId",
                table: "SolutionFiles",
                column: "UserId");

           // MigrateOldSourceCodeData(migrationBuilder); //Not needed anymore. All sources have been migrated on all environments
        }

        private void MigrateOldSourceCodeData(MigrationBuilder migrationBuilder)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = typeof(AddSolutionFiles).Namespace + ".20200129153803_AddSolutionFiles.MoveDataScript.sql";
            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            using StreamReader reader = new StreamReader(stream);
            string sqlResult = reader.ReadToEnd();
            migrationBuilder.Sql(sqlResult);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SolutionFiles");
        }
    }
}
