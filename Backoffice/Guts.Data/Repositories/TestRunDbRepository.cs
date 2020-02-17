using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Business.Repositories;
using Guts.Domain.TestRunAggregate;

namespace Guts.Data.Repositories
{
    public class TestRunDbRepository : BaseDbRepository<TestRun>, ITestRunRepository
    {
        public TestRunDbRepository(GutsContext context) : base(context)
        {
        }

        public async Task<IList<TestRun>> GetUserTestRunsForAssignmentAsync(int assignmentId, int userId, DateTime? date)
        {
            var query = from assignment in _context.Assignments
                        from testrun in assignment.TestRuns
                        where (assignment.Id == assignmentId) &&
                              (testrun.UserId == userId) &&
                              (date == null || testrun.CreateDateTime <= date)
                        orderby testrun.CreateDateTime
                        select testrun;

            return await query.ToListAsync();
        }

        public async Task<IList<TestRun>> GetLastTestRunForAssignmentOfAllUsersAsync(int assignmentId)
        {
            var lastTestRunsDataQuery = from assignment in _context.Assignments
                from testrun in assignment.TestRuns
                where assignment.Id == assignmentId
                orderby testrun.CreateDateTime descending
                group testrun by new {testrun.AssignmentId, testrun.UserId} into testRunGroups
                select new
                {
                    testRunGroups.Key.AssignmentId,
                    testRunGroups.Key.UserId,
                    CreateDateTime = testRunGroups.Max(tr => tr.CreateDateTime)
                };

            var lastTestRunsQuery = from testrun in _context.TestRuns
                from testrunData in lastTestRunsDataQuery
                where testrun.AssignmentId == testrunData.AssignmentId
                      && testrun.UserId == testrunData.UserId
                      && testrun.CreateDateTime == testrunData.CreateDateTime
                select testrun;

            var lastTestRuns = await lastTestRunsQuery.Include(testrun => testrun.User).ToListAsync();
            return lastTestRuns;
        }

        public async Task<IList<TestRun>> GetTeamTestRunsForAssignmentAsync(int assignmentId, int teamId, DateTime? dateUtc)
        {
            var query = from assignment in _context.Assignments
                from testrun in assignment.TestRuns
                join teamUser in _context.ProjectTeamUsers on testrun.UserId equals teamUser.UserId
                where (assignment.Id == assignmentId) &&
                      (teamUser.ProjectTeamId == teamId) &&
                      (dateUtc == null || testrun.CreateDateTime <= dateUtc)
                orderby testrun.CreateDateTime
                select testrun;

            return await query.ToListAsync();
        }
    }
}