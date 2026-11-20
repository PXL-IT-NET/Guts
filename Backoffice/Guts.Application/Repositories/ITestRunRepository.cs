using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Guts.Domain.TestRunAggregate;

namespace Guts.Application.Repositories;

public sealed record PruningAssignmentTarget(int AssignmentId, int TopicId, bool IsProject);

public sealed record TestRunBatchDeletionResult(int TestRunsDeleted, int TestResultsDeleted);

public interface ITestRunRepository : IBasicRepository<TestRun>
{
    Task<IList<TestRun>> GetUserTestRunsForAssignmentAsync(int assignmentId, int userId, DateTime? date);

    Task<IList<TestRun>> GetLastTestRunForAssignmentOfAllUsersAsync(int assignmentId);

    Task RemoveAllTopicTestRunsOfUserAsync(int topicId, int userId);

    Task<IList<TestRun>> GetTeamTestRunsForAssignmentAsync(int assignmentId, int teamId, DateTime? dateUtc);

    Task<IReadOnlyList<PruningAssignmentTarget>> GetPruningAssignmentTargetsForPeriodAsync(
        int periodId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TestRunSnapshot>> GetTestRunSnapshotsForAssignmentAsync(
        int assignmentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, int>> GetProjectTeamMembershipMapAsync(
        int projectId,
        CancellationToken cancellationToken = default);

    Task<TestRunBatchDeletionResult> DeleteTestRunsBatchAsync(
        IReadOnlyCollection<int> testRunIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TestRunSnapshot>> GetUserTestRunSnapshotsForAssignmentAsync(
        int assignmentId,
        int userId,
        DateTime? dateUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TestRunSnapshot>> GetTeamTestRunSnapshotsForAssignmentAsync(
        int assignmentId,
        int teamId,
        DateTime? dateUtc,
        CancellationToken cancellationToken = default);
}
