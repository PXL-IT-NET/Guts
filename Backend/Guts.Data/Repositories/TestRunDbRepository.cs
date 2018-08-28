using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Domain;
using Microsoft.EntityFrameworkCore;

namespace Guts.Data.Repositories
{
    public class TestRunDbRepository : BaseDbRepository<TestRun>, ITestRunRepository
    {
        public TestRunDbRepository(GutsContext context) : base(context)
        {
        }

        public async Task<IList<TestRun>> GetUserTestRunsForExercise(int exerciseId, int userId)
        {
            var query = from exercise in _context.Exercises
                from testrun in exercise.TestRuns
                where exercise.Id == exerciseId && testrun.UserId == userId
                orderby testrun.CreateDateTime
                select testrun;

            return await query.ToListAsync();
        }
    }
}