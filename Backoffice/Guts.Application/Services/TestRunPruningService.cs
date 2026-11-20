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

        var projectTeamMaps = new Dictionary<int, IReadOnlyDictionary<int, int>>();
        int assignmentsScanned = 0;
        int totalRunsDeleted = 0;
        int totalResultsDeleted = 0;

        foreach (PruningAssignmentTarget target in targets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            assignmentsScanned++;

            IReadOnlyList<TestRunSnapshot> snapshots =
                await testRunRepository.GetTestRunSnapshotsForAssignmentAsync(target.AssignmentId, cancellationToken);

            if (snapshots.Count <= 1)
            {
                continue;
            }

            IReadOnlyDictionary<int, int> teamMap = target.IsProject
                ? await GetOrLoadTeamMapAsync(target.TopicId, projectTeamMaps, cancellationToken)
                : null;

            IReadOnlyList<int> redundantRunIds = TestRunPruningPolicy.FindRedundantTestRunIds(snapshots, teamMap);
            if (redundantRunIds.Count == 0)
            {
                continue;
            }

            foreach (int[] batch in redundantRunIds.Chunk(batchSize))
            {
                cancellationToken.ThrowIfCancellationRequested();
                TestRunBatchDeletionResult batchResult =
                    await testRunRepository.DeleteTestRunsBatchAsync(batch, cancellationToken);

                totalRunsDeleted += batchResult.TestRunsDeleted;
                totalResultsDeleted += batchResult.TestResultsDeleted;
            }
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
