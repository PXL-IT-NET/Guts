using System.Text;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Guts.Data.Migrations
{
    public partial class StoredProcLastTestResults : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sb = new StringBuilder();
            sb.AppendLine("CREATE PROCEDURE sp_getLastTestResultsOfAssignment(IN assignmentId INT, IN userId INT, IN maxCreateDateTime DATETIME) ");
            sb.AppendLine("LANGUAGE SQL");
            sb.AppendLine("NOT DETERMINISTIC");
            sb.AppendLine("CONTAINS SQL");
            sb.AppendLine("SQL SECURITY DEFINER");
            sb.AppendLine("COMMENT ''");
            sb.AppendLine("BEGIN");
            sb.AppendLine("select tr.*");
            sb.AppendLine("from (");
            sb.AppendLine(" select tr.TestId as TestId, tr.UserId as UserId, max(tr.CreateDateTime) as CreateDateTime");
            sb.AppendLine(" from testresults tr ");
            sb.AppendLine(" inner join tests t on tr.TestId = t.Id");
            sb.AppendLine(" where t.AssignmentId = assignmentId ");
            sb.AppendLine("  and (maxCreateDateTime is null or tr.CreateDateTime <= maxCreateDateTime)");
            sb.AppendLine("  and (userId is null or tr.UserId = userId)");
            sb.AppendLine(" group by tr.TestId, tr.UserId) as g");
            sb.AppendLine("inner join testresults tr on tr.TestId = g.TestId and tr.UserId = g.UserId and tr.CreateDateTime = g.CreateDateTime;");
            sb.AppendLine(@"END");
            migrationBuilder.Sql(sb.ToString());
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE sp_getLastTestResultsOfAssignment;");
        }
    }
}
