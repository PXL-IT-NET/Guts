using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Guts.Application.Dtos;
using Guts.Application.Repositories;
using Guts.Common;
using Guts.Domain.PeriodAggregate;
using Guts.Domain.TestRunAggregate;
using Microsoft.Extensions.Logging;

namespace Guts.Application.Services;

internal class TestRunPruningService(
    IPeriodRepository periodRepository,
    ITestRunRepository testRunRepository,
    ILogger<TestRunPruningService> logger) : ITestRunPruningService
{
    public async Task<TestRunPruningResult> PruneRedundantTestRunsAsync(
        int batchSize = ITestRunPruningService.DefaultBatchSize,
        CancellationToken cancellationToken = default)
    {
        Contracts.Require(batchSize > 0, "The pruning batch size must be a positive number.");

        var stopwatch = Stopwatch.StartNew();
        IPeriod activePeriod = await TryGetActivePeriodAsync();
        if (activePeriod == null || activePeriod.IsDeleting)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Skipping test run pruning because no active non-deleting period was found.");
            }

            return new TestRunPruningResult(0, 0, 0);
        }

        IReadOnlyList<PruningAssignmentTarget> targets =
            await testRunRepository.GetPruningAssignmentTargetsForPeriodAsync(activePeriod.Id, cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Scanning {TargetCount} assignment(s) with multiple test runs in active period {PeriodId} (batch size: {BatchSize}).",
                targets.Count,
                activePeriod.Id,
                batchSize);
        }

        var projectTeamMaps = new Dictionary<int, IReadOnlyDictionary<int, int>>();
        int assignmentsScanned = 0;
        int totalRunsDeleted = 0;
        int totalResultsDeleted = 0;

        foreach (PruningAssignmentTarget target in targets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            assignmentsScanned++;

            TestRunBatchDeletionResult assignmentResult =
                await PruneAssignmentAsync(target, batchSize, projectTeamMaps, cancellationToken);

            totalRunsDeleted += assignmentResult.TestRunsDeleted;
            totalResultsDeleted += assignmentResult.TestResultsDeleted;
        }

        stopwatch.Stop();

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Pruned redundant test runs for active period {PeriodId}: scanned {AssignmentsScanned} assignments, deleted {TestRunsDeleted} test runs and {TestResultsDeleted} test results in {ElapsedMs} ms.",
                activePeriod.Id,
                assignmentsScanned,
                totalRunsDeleted,
                totalResultsDeleted,
                stopwatch.ElapsedMilliseconds);
        }

        return new TestRunPruningResult(assignmentsScanned, totalRunsDeleted, totalResultsDeleted);
    }

    private async Task<TestRunBatchDeletionResult> PruneAssignmentAsync(
        PruningAssignmentTarget target,
        int batchSize,
        Dictionary<int, IReadOnlyDictionary<int, int>> projectTeamMaps,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<TestRunSnapshot> snapshots =
            await testRunRepository.GetTestRunSnapshotsForAssignmentAsync(target.AssignmentId, cancellationToken);

        if (snapshots.Count <= 1)
        {
            return new TestRunBatchDeletionResult(0, 0);
        }

        IReadOnlyDictionary<int, int> teamMap = target.IsProject
            ? await GetOrLoadTeamMapAsync(target.TopicId, projectTeamMaps, cancellationToken)
            : null;

        IReadOnlyList<int> redundantRunIds = TestRunPruningPolicy.FindRedundantTestRunIds(snapshots, teamMap);
        if (redundantRunIds.Count == 0)
        {
            return new TestRunBatchDeletionResult(0, 0);
        }

        int assignmentRunsDeleted = 0;
        int assignmentResultsDeleted = 0;
        foreach (int[] batch in redundantRunIds.Chunk(batchSize))
        {
            cancellationToken.ThrowIfCancellationRequested();
            TestRunBatchDeletionResult batchResult =
                await testRunRepository.DeleteTestRunsBatchAsync(batch, cancellationToken);

            assignmentRunsDeleted += batchResult.TestRunsDeleted;
            assignmentResultsDeleted += batchResult.TestResultsDeleted;
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Assignment {AssignmentId}: deleted {TestRunsDeleted} redundant test run(s) and {TestResultsDeleted} test result(s) out of {TotalRuns} run(s).",
                target.AssignmentId,
                assignmentRunsDeleted,
                assignmentResultsDeleted,
                snapshots.Count);
        }

        return new TestRunBatchDeletionResult(assignmentRunsDeleted, assignmentResultsDeleted);
    }

    private async Task<IPeriod> TryGetActivePeriodAsync()
    {
        try
        {
            return await periodRepository.GetPeriodAsync();
        }
        catch (DataNotFoundException)
        {
            return null;
        }
    }

    private async Task<IReadOnlyDictionary<int, int>> GetOrLoadTeamMapAsync(
        int projectId,
        Dictionary<int, IReadOnlyDictionary<int, int>> cache,
        CancellationToken cancellationToken)
    {
        if (!cache.TryGetValue(projectId, out IReadOnlyDictionary<int, int> teamMap))
        {
            teamMap = await testRunRepository.GetProjectTeamMembershipMapAsync(projectId, cancellationToken);
            cache[projectId] = teamMap;
        }

        return teamMap;
    }
}
