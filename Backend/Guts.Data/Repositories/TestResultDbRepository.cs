using Guts.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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
            var results = _context.TestResults.FromSql(
                "CALL sp_getLastTestResultsOfUser({0}, {1}, {2})", assignmentId, null, dateUtc);

            return await results.AsNoTracking().ToListAsync();
        }

        public async Task<IList<TestResult>> GetLastTestResultsOfUser(int assignmentId, int userId, DateTime? dateUtc)
        {
            var results = _context.TestResults.FromSql(
                "CALL sp_getLastTestResultsOfUser({0}, {1}, {2})", assignmentId, userId, dateUtc);

            return await results.AsNoTracking().ToListAsync();
        }

        public async Task<IList<TestResult>> GetLastTestResultsOfTeam(int assignmentId, int teamId, DateTime? dateUtc)
        {
            var results = _context.TestResults.FromSql(
                "CALL sp_getLastTestResultsOfTeam({0}, {1}, {2})", assignmentId, teamId, dateUtc);

            return await results.AsNoTracking().ToListAsync();
        }
    }
}