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

        public Task<IList<AssignmentWithLastResultsOfMultipleUsers>> GetLastTestResultsOfChapterAsync(int chapterId, DateTime? dateUtc)
        {
            var query = from exercise in _context.Exercises
                        from test in exercise.Tests
                        from testResult in test.Results
                        where exercise.ChapterId == chapterId
                              && (dateUtc == null || testResult.CreateDateTime <= dateUtc)
                        group new { exercise, test, testResult } by exercise.Id into exerciseGroups
                        let assignment = exerciseGroups.FirstOrDefault().exercise
                        select new AssignmentWithLastResultsOfMultipleUsers
                        {
                            Assignment = assignment,
                            TestsWithLastResultOfMultipleUsers = from exerciseGroup in exerciseGroups
                                                                 group exerciseGroup by new { exerciseGroup.test.Id } into testGroups
                                                                 let test = testGroups.FirstOrDefault().test
                                                                 select new TestWithLastResultOfMultipleUsers
                                                                 {
                                                                     Test = test,
                                                                     TestResults = from testGroup in testGroups
                                                                                   group testGroup by testGroup.testResult.UserId into userTestResultGroups
                                                                                   select userTestResultGroups.OrderByDescending(g => g.testResult.CreateDateTime).FirstOrDefault().testResult
                                                                 }
                        };

            //Could not use ToListAsync here because it generates a strange error...
            return Task.FromResult<IList<AssignmentWithLastResultsOfMultipleUsers>>(query.AsNoTracking().ToList());
        }

        public Task<IList<AssignmentWithLastResultsOfUser>> GetLastTestResultsOfChapterAsync(int chapterId, int userId, DateTime? dateUtc)
        {
            var query = from exercise in _context.Exercises
                        from test in exercise.Tests
                        from testResult in test.Results
                        where testResult.UserId == userId
                              && exercise.ChapterId == chapterId
                              && (dateUtc == null || testResult.CreateDateTime <= dateUtc)
                        group new { exercise, test, testResult } by exercise.Id into exerciseGroups
                        let assignment = exerciseGroups.FirstOrDefault().exercise
                        select new AssignmentWithLastResultsOfUser
                        {
                            Assignment = assignment,
                            TestsWithLastResultOfUser = from exerciseGroup in exerciseGroups
                                                        group exerciseGroup by new { exerciseGroup.test.Id } into testGroups
                                                        let test = testGroups.FirstOrDefault().test
                                                        select new TestWithLastResultOfUser
                                                        {
                                                            Test = test,
                                                            TestResult = testGroups.OrderByDescending(g => g.testResult.CreateDateTime).FirstOrDefault().testResult
                                                        }
                        };

            //Could not use ToListAsync here because it generates a strange error...
            return Task.FromResult<IList<AssignmentWithLastResultsOfUser>>(query.AsNoTracking().ToList());
        }

        public Task<AssignmentWithLastResultsOfUser> GetLastTestResultsOfExerciseAsync(int exerciseId, int userId, DateTime? dateUtc)
        {
            var query = from exercise in _context.Exercises
                        from test in exercise.Tests
                        from testResult in test.Results
                        where testResult.UserId == userId
                              && (test.AssignmentId == exerciseId)
                              && (dateUtc == null || testResult.CreateDateTime <= dateUtc)
                        group new { exercise, test, testResult } by exercise.Id into exerciseGroups
                        let assignment = exerciseGroups.FirstOrDefault().exercise
                        select new AssignmentWithLastResultsOfUser
                        {
                            Assignment = assignment,
                            TestsWithLastResultOfUser = from exerciseGroup in exerciseGroups
                                                        group exerciseGroup by new { exerciseGroup.test.Id }
                                into testGroups
                                                        let test = testGroups.FirstOrDefault().test
                                                        select new TestWithLastResultOfUser
                                                        {
                                                            Test = test,
                                                            TestResult = testGroups.OrderByDescending(g => g.testResult.CreateDateTime).FirstOrDefault().testResult
                                                        }
                        };

            //Could not use ToListAsync here because it generates a strange error...
            return Task.FromResult(query.AsNoTracking().FirstOrDefault());
        }
    }
}