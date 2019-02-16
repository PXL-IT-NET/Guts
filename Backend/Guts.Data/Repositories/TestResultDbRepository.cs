using Guts.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Guts.Data.Repositories
{
    public class TestResultDbRepository : BaseDbRepository<TestResult>, ITestResultRepository
    {
        public TestResultDbRepository(GutsContext context) : base(context)
        {
        }

        public async Task<IList<TestResult>> GetLastTestResults(int assignmentId, DateTime? dateUtc)
        {
            return await GetLastTestResults(assignmentId, null, dateUtc);
        }

        public async Task<IList<TestResult>> GetLastTestResultsOfUser(int assignmentId, int userId, DateTime? dateUtc)
        {
            return await GetLastTestResults(assignmentId, userId, dateUtc);
        }

        public async Task<IList<TestResult>> GetLastTestResultsOfTeam(int assignmentId, int teamId, DateTime? dateUtc)
        {
            var testresultsPerTestPerTeamQuery = from testresult in _context.TestResults
                                                 join projectTeamUser in _context.ProjectTeamUsers on testresult.UserId equals projectTeamUser.UserId
                                                 where (testresult.Test.AssignmentId == assignmentId)
                                                       && (projectTeamUser.ProjectTeamId == teamId)
                                                       && (dateUtc == null || testresult.CreateDateTime <= dateUtc)
                                                 group testresult by testresult.TestId;

            var lastResultsQuery = testresultsPerTestPerTeamQuery.Select(testresultGroup =>
                testresultGroup.OrderByDescending(testresult => testresult.CreateDateTime).FirstOrDefault());

            return await lastResultsQuery.AsNoTracking().ToListAsync();
        }

        private async Task<IList<TestResult>> GetLastTestResults(int assignmentId, int? userId, DateTime? dateUtc)
        {
            var testresultsPerTestPerUserQuery = from testresult in _context.TestResults
                                                 where (testresult.Test.AssignmentId == assignmentId)
                                                       && (dateUtc == null || testresult.CreateDateTime <= dateUtc)
                                                       && (userId == null || testresult.UserId == userId.Value)
                                                 group testresult by new { testresult.TestId, testresult.UserId };

            var lastResultsQuery = testresultsPerTestPerUserQuery.Select(testresultGroup =>
                testresultGroup.OrderByDescending(testresult => testresult.CreateDateTime).FirstOrDefault());

            return await lastResultsQuery.AsNoTracking().ToListAsync();
        }
    }
}