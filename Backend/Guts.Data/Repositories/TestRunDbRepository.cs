using Guts.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Guts.Data.Repositories
{
    public class TestRunDbRepository : BaseDbRepository<TestRun>, ITestRunRepository
    {
        public TestRunDbRepository(GutsContext context) : base(context)
        {
        }

        public async Task<IList<TestRun>> GetUserTestRunsForExercise(int exerciseId, int userId, DateTime? date)
        {
            var query = from exercise in _context.Exercises
                        from testrun in exercise.TestRuns
                        where (exercise.Id == exerciseId) &&
                              (testrun.UserId == userId) &&
                              (date == null || testrun.CreateDateTime <= date)
                        orderby testrun.CreateDateTime
                        select testrun;

            return await query.ToListAsync();
        }

        public async Task<IList<TestRun>> GetLastTestRunForExerciseOfAllUsers(int exerciseId)
        {
            var query = from exercise in _context.Exercises
                from testrun in exercise.TestRuns
                where exercise.Id == exerciseId
                orderby testrun.CreateDateTime descending
                group testrun by testrun.User
                into userGroups
                select new
                {
                    TestRun = userGroups.FirstOrDefault(),
                    User = userGroups.Key
                };

            var results = await query.ToListAsync();
            foreach (var result in results) //For some reason including the USER relation does not work out of the box
            {
                result.TestRun.User = result.User;
            }

            return results.Select(r => r.TestRun).ToList();
        }
    }
}