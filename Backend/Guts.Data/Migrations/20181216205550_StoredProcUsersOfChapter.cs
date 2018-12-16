using System.Text;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Guts.Data.Migrations
{
    public partial class StoredProcUsersOfChapter : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sb = new StringBuilder();
            sb.AppendLine("CREATE PROCEDURE sp_getUsersOfChapter(IN ChapterId INT) ");
            sb.AppendLine("LANGUAGE SQL");
            sb.AppendLine("NOT DETERMINISTIC");
            sb.AppendLine("CONTAINS SQL");
            sb.AppendLine("SQL SECURITY DEFINER");
            sb.AppendLine("COMMENT ''");
            sb.AppendLine("BEGIN");
            sb.AppendLine("select u.*");
            sb.AppendLine("from (");
            sb.AppendLine(" select u.Id");
            sb.AppendLine(" from Users u");
            sb.AppendLine(" inner join TestRuns tr on tr.UserId = u.Id");
            sb.AppendLine(" inner join Assignments a on tr.AssignmentId = a.Id");
            sb.AppendLine(" where a.ChapterId = ChapterId");
            sb.AppendLine(" group by u.Id) as g");
            sb.AppendLine(" inner join Users u on u.Id = g.Id;");
            sb.AppendLine(@"END");
            migrationBuilder.Sql(sb.ToString());
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE sp_getUsersOfChapter;");
        }
    }
}
