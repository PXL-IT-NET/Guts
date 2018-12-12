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

        public async Task<IList<TestResult>> GetLastTestResultsOfExerciseAsync(int exerciseId, int? userId, DateTime? dateUtc)
        {
            var results = _context.TestResults.FromSql(
                "CALL sp_getLastTestResultsOfAssignment({0}, {1}, {2})", exerciseId, userId, dateUtc);

            return await results.AsNoTracking().ToListAsync();
        }
    }
}