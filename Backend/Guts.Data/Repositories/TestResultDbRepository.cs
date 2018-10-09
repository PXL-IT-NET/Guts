using Guts.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Guts.Data.Repositories
{
    public class TestResultDbRepository : BaseDbRepository<TestRun>, ITestResultRepository
    {
        public TestResultDbRepository(GutsContext context) : base(context)
        {
        }

        public Task<IList<TestWithLastUserResults>> GetLastTestResultsOfChapterAsync(int chapterId, int? userId, DateTime? dateUtc)
        {

            var lastUserTestResultsQuery = from exercise in _context.Exercises
                                           from test in exercise.Tests
                                           from testResult in test.Results
                                           where (userId == null || testResult.UserId == userId.Value)
                                                 && (exercise.ChapterId == chapterId)
                                                 && (dateUtc == null || testResult.CreateDateTime <= dateUtc)
                                           group new { testResult, test } by new { testResult.UserId, test.Id } into userTestRunGroup
                                           select userTestRunGroup.OrderByDescending(g => g.testResult.CreateDateTime).FirstOrDefault();

            var query = from testWithLastUserResult in lastUserTestResultsQuery
                        group testWithLastUserResult by testWithLastUserResult.test.Id
                into testGroup
                        let test = testGroup.FirstOrDefault().test
                        select new TestWithLastUserResults
                        {
                            Test = test,
                            ResultsOfUsers = testGroup.Select(g => g.testResult)
                        };

            //Could not use ToListAsync here because it generates a strange error...
            return Task.FromResult<IList<TestWithLastUserResults>>(query.AsNoTracking().ToList());
        }

        public Task<IList<TestWithLastUserResults>> GetLastTestResultsOfExerciseAsync(int exerciseId, int userId, DateTime? dateUtc)
        {
            var lastUserTestResultsQuery = from test in _context.Tests
                from testResult in test.Results
                where testResult.UserId == userId
                      && (test.ExerciseId == exerciseId)
                      && (dateUtc == null || testResult.CreateDateTime <= dateUtc)
                group new {testResult, test} by new {testResult.UserId, test.Id}
                into userTestRunGroup
                select userTestRunGroup.OrderByDescending(g => g.testResult.CreateDateTime).FirstOrDefault();

            var query = from testWithLastUserResult in lastUserTestResultsQuery
                group testWithLastUserResult by testWithLastUserResult.test.Id
                into testGroup
                let test = testGroup.FirstOrDefault().test
                select new TestWithLastUserResults
                {
                    Test = test,
                    ResultsOfUsers = testGroup.Select(g => g.testResult)
                };

            //Could not use ToListAsync here because it generates a strange error...
            return Task.FromResult<IList<TestWithLastUserResults>>(query.AsNoTracking().ToList());
        }
    }
}