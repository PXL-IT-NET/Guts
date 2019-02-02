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
            var query = from assignment in _context.Assignments
                from testrun in assignment.TestRuns
                where assignment.Id == assignmentId
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