using System.Text;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Guts.Data.Migrations
{
    public partial class StoredProcTestResultsOfTeam : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE sp_getLastTestResultsOfAssignment;");

            var sb = new StringBuilder();
            sb.AppendLine("CREATE PROCEDURE sp_getLastTestResultsOfUser(IN assignmentId INT, IN userId INT, IN maxCreateDateTime DATETIME) ");
            sb.AppendLine("LANGUAGE SQL");
            sb.AppendLine("NOT DETERMINISTIC");
            sb.AppendLine("CONTAINS SQL");
            sb.AppendLine("SQL SECURITY DEFINER");
            sb.AppendLine("COMMENT ''");
            sb.AppendLine("BEGIN");
            sb.AppendLine("select tr.*");
            sb.AppendLine("from (");
            sb.AppendLine(" select tr.TestId as TestId, tr.UserId as UserId, max(tr.CreateDateTime) as CreateDateTime");
            sb.AppendLine(" from TestResults tr ");
            sb.AppendLine(" inner join Tests t on tr.TestId = t.Id");
            sb.AppendLine(" where t.AssignmentId = assignmentId ");
            sb.AppendLine("  and (maxCreateDateTime is null or tr.CreateDateTime <= maxCreateDateTime)");
            sb.AppendLine("  and (userId is null or tr.UserId = userId)");
            sb.AppendLine(" group by tr.TestId, tr.UserId) as g");
            sb.AppendLine("inner join TestResults tr on tr.TestId = g.TestId and tr.UserId = g.UserId and tr.CreateDateTime = g.CreateDateTime;");
            sb.AppendLine(@"END");
            migrationBuilder.Sql(sb.ToString());

            sb = new StringBuilder();
            sb.AppendLine("CREATE PROCEDURE sp_getLastTestResultsOfTeam(IN assignmentId INT, IN teamId INT, IN maxCreateDateTime DATETIME) ");
            sb.AppendLine("LANGUAGE SQL");
            sb.AppendLine("NOT DETERMINISTIC");
            sb.AppendLine("CONTAINS SQL");
            sb.AppendLine("SQL SECURITY DEFINER");
            sb.AppendLine("COMMENT ''");
            sb.AppendLine("BEGIN");
            sb.AppendLine("select tr.*");
            sb.AppendLine("from (");
            sb.AppendLine(" select tr.TestId as TestId, ptu.ProjectTeamId as ProjectTeamId, max(tr.CreateDateTime) as CreateDateTime");
            sb.AppendLine(" from TestResults tr ");
            sb.AppendLine(" inner join Tests t on tr.TestId = t.Id");
            sb.AppendLine(" inner join ProjectTeamUsers ptu on tr.UserId = ptu.UserId");
            sb.AppendLine(" where t.AssignmentId = assignmentId ");
            sb.AppendLine("  and (maxCreateDateTime is null or tr.CreateDateTime <= maxCreateDateTime)");
            sb.AppendLine("  and (teamId is null or ptu.ProjectTeamId = teamId)");
            sb.AppendLine(" group by tr.TestId, ptu.ProjectTeamId) as g");
            sb.AppendLine("inner join TestResults tr on tr.TestId = g.TestId and tr.CreateDateTime = g.CreateDateTime");
            sb.AppendLine("inner join ProjectTeamUsers ptu on ptu.UserId = tr.UserId");
            sb.AppendLine("where ptu.ProjectTeamId = teamId;");
            sb.AppendLine(@"END");
            migrationBuilder.Sql(sb.ToString());
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE sp_getLastTestResultsOfUser;");

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
            sb.AppendLine(" from TestResults tr ");
            sb.AppendLine(" inner join Tests t on tr.TestId = t.Id");
            sb.AppendLine(" where t.AssignmentId = assignmentId ");
            sb.AppendLine("  and (maxCreateDateTime is null or tr.CreateDateTime <= maxCreateDateTime)");
            sb.AppendLine("  and (userId is null or tr.UserId = userId)");
            sb.AppendLine(" group by tr.TestId, tr.UserId) as g");
            sb.AppendLine("inner join TestResults tr on tr.TestId = g.TestId and tr.UserId = g.UserId and tr.CreateDateTime = g.CreateDateTime;");
            sb.AppendLine(@"END");
            migrationBuilder.Sql(sb.ToString());

            migrationBuilder.Sql("DROP PROCEDURE sp_getLastTestResultsOfTeam;");
        }
    }
}
