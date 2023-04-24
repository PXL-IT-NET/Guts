using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain.TestRunAggregate;

namespace Guts.Business.Repositories
{
    public interface ITestRunRepository : IBasicRepository<TestRun>
    {
        Task<IList<TestRun>> GetUserTestRunsForAssignmentAsync(int assignmentId, int userId, DateTime? date);

        Task<IList<TestRun>> GetLastTestRunForAssignmentOfAllUsersAsync(int assignmentId);

        Task RemoveAllTopicTestRunsOfUserAsync(int topicId, int userId);

        Task<IList<TestRun>> GetTeamTestRunsForAssignmentAsync(int assignmentId, int teamId, DateTime? dateUtc);
    }
}