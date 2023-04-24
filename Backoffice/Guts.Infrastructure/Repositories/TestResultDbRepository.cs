using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Business.Repositories;
using Guts.Domain.TestRunAggregate;
using Microsoft.EntityFrameworkCore;

namespace Guts.Infrastructure.Repositories
{
    internal class TestResultDbRepository : BaseDbRepository<TestResult, TestResult>, ITestResultRepository
    {
        public TestResultDbRepository(GutsContext context) : base(context)
        {
        }

        public async Task<IList<TestResult>> GetLastTestResultsOfAllUsers(int assignmentId, DateTime? dateUtc)
        {
            return await GetLastTestResultsPerUser(assignmentId, null, dateUtc);
        }

        public async Task<IList<TestResult>> GetLastTestResultsOfUser(int assignmentId, int userId, DateTime? dateUtc)
        {
            return await GetLastTestResultsPerUser(assignmentId, userId, dateUtc);
        }

        public async Task<IList<TestResult>> GetLastTestResultsOfAllTeams(int projectId, int assignmentId, DateTime? dateUtc)
        {
            return await GetLastTestResultsPerTeam(projectId, assignmentId, null, dateUtc);
        }

        public async Task<IList<TestResult>> GetLastTestResultsOfTeam(int projectId, int assignmentId, int teamId, DateTime? dateUtc)
        {
            return await GetLastTestResultsPerTeam(projectId, assignmentId, teamId, dateUtc);
        }

        public async Task<IList<TestResult>> GetLastTestResultsOfAssignmentsAsync(int[] assignmentIds, DateTime? dateUtc)
        {
            var lastResultKeys = from testresult in _context.TestResults
                                 where assignmentIds.Contains(testresult.Test.AssignmentId)
                                       && (dateUtc == null || testresult.CreateDateTime <= dateUtc)
                                 group testresult by new { testresult.TestId, testresult.UserId } into g
                                 select new
                                 {
                                     g.Key.TestId,
                                     g.Key.UserId,
                                     CreatDateTime = g.Max(tr => tr.CreateDateTime)
                                 };

            var lastResultsQuery = from testresult in _context.TestResults
                                   from key in lastResultKeys
                                   where testresult.TestId == key.TestId
                                         && testresult.UserId == key.UserId
                                         && testresult.CreateDateTime == key.CreatDateTime
                                   select testresult;

            return await lastResultsQuery.Include(testresult => testresult.Test).AsNoTracking().ToListAsync();
        }

        private async Task<IList<TestResult>> GetLastTestResultsPerTeam(int projectId, int assignmentId, int? teamId, DateTime? dateUtc)
        {
            var lastResultKeys = from testresult in _context.TestResults
                from projectTeamUser in _context.ProjectTeamUsers //on testresult.UserId equals projectTeamUser.UserId
                where (testresult.Test.AssignmentId == assignmentId)
                      && (testresult.UserId == projectTeamUser.UserId)
                      && (dateUtc == null || testresult.CreateDateTime <= dateUtc)
                      && (teamId == null || projectTeamUser.ProjectTeamId == teamId)
                      && (projectTeamUser.ProjectTeam.ProjectId == projectId)
                group testresult by new { testresult.TestId, projectTeamUser.ProjectTeamId } into g
                select new
                {
                    g.Key.TestId,
                    g.Key.ProjectTeamId,
                    CreatDateTime = g.Max(tr => tr.CreateDateTime)
                };


            var test = lastResultKeys.ToList();

            var lastResultsQuery = from testresult in _context.TestResults
                                   join projectTeamUser in _context.ProjectTeamUsers on testresult.UserId equals projectTeamUser.UserId
                                   from key in lastResultKeys
                                   where testresult.TestId == key.TestId
                                         && projectTeamUser.ProjectTeamId == key.ProjectTeamId
                                         && projectTeamUser.ProjectTeam.ProjectId == projectId
                                         && testresult.CreateDateTime == key.CreatDateTime
                                         && testresult.Test.AssignmentId == assignmentId
                                   select testresult;

            return await lastResultsQuery.AsNoTracking().ToListAsync();
        }

        private async Task<IList<TestResult>> GetLastTestResultsPerUser(int assignmentId, int? userId, DateTime? dateUtc)
        {
            var lastResultKeys = from testresult in _context.TestResults
                                 where testresult.Test.AssignmentId == assignmentId
                                       && (dateUtc == null || testresult.CreateDateTime <= dateUtc)
                                       && (userId == null || testresult.UserId == userId.Value)
                                 group testresult by new { testresult.TestId, testresult.UserId } into g
                                 select new
                                 {
                                     g.Key.TestId,
                                     g.Key.UserId,
                                     CreatDateTime = g.Max(tr => tr.CreateDateTime)
                                 };

            var lastResultsQuery = from testresult in _context.TestResults
                                   from key in lastResultKeys
                                   where testresult.TestId == key.TestId
                                         && testresult.UserId == key.UserId
                                         && testresult.CreateDateTime == key.CreatDateTime
                                         && testresult.Test.AssignmentId == assignmentId
                                   select testresult;

            return await lastResultsQuery.AsNoTracking().ToListAsync();
        }
    }
}