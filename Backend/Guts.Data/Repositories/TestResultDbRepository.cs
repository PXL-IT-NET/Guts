using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Domain;
using Microsoft.EntityFrameworkCore;

namespace Guts.Data.Repositories
{
    public class TestResultDbRepository : BaseDbRepository<TestRun>, ITestResultRepository
    {
        public TestResultDbRepository(GutsContext context) : base(context)
        {
        }

        public Task<IList<TestWithLastUserResults>> GetLastTestResultsOfChapterAsync(int chapterId, int? userId)
        {
            var lastTestRunOfUsersQuery = from exercise in _context.Exercises
                from testRun in exercise.TestRuns
                where (userId == null || testRun.UserId == userId.Value)
                group testRun by testRun.UserId into userTestRunGroup
                select userTestRunGroup.OrderByDescending(g => g.CreateDateTime).FirstOrDefault();

            var query = from exercise in _context.Exercises
                from test in exercise.Tests
                from testRun in lastTestRunOfUsersQuery
                from testResult in testRun.TestResults
                where (exercise.ChapterId == chapterId) 
                    && (testResult.TestId == test.Id) 
                    && (testRun.ExerciseId == exercise.Id)
                orderby exercise.Number, test.TestName
                group new {test, testRun, testResult} by test.Id
                into testGroup
                let test = testGroup.FirstOrDefault().test
                select new TestWithLastUserResults
                {
                    Test = test,
                    NumberOfUsers = testGroup.GroupBy(g => g.testRun.UserId).Count(),
                    ResultsOfUsers = testGroup.Select(g => g.testResult)
                };

            //Could not use ToListAsync here because it generates a strange error...
            return Task.FromResult<IList<TestWithLastUserResults>>(query.AsNoTracking().ToList());
        }
    }
}