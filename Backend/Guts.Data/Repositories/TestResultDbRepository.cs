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

        public async Task<IList<TestWithLastUserResults>> GetLastTestResultsOfChapterAsync(int chapterId, int? userId)
        {
            var lastTestRunOfUsersQuery = from exercise in _context.Exercises
                from testRun in exercise.TestRuns
                where (userId == null || userId.Value == testRun.UserId) 
                group testRun by testRun.UserId into userTestRunGroup
                select userTestRunGroup.OrderByDescending(g => g.CreateDateTime).FirstOrDefault();

            var query = from exercise in _context.Exercises
                from test in _context.Tests
                from testRun in lastTestRunOfUsersQuery
                from testResult in testRun.TestResults
                where (exercise.ChapterId == chapterId) && (testResult.TestId == test.Id)
                orderby exercise.Number, test.TestName
                group new {test, testRun, testResult} by test.Id
                into testGroup
                let test = testGroup.FirstOrDefault().test
                select new TestWithLastUserResults
                {
                    Test = test,
                    ResultsOfUsers = testGroup.Select(g => g.testResult)
                };

            return await query.ToListAsync();
        }
    }
}