using System;
using System.Collections.Generic;
using System.Linq;

namespace Guts.Domain.TestRunAggregate;

public sealed record TestResultSnapshot(int TestId, bool Passed);

public sealed record TestRunSnapshot(
    int TestRunId,
    int UserId,
    DateTime CreateDateTime,
    IReadOnlyList<TestResultSnapshot> Results);

public static class TestRunPruningPolicy
{
    public static IReadOnlyList<int> FindRedundantTestRunIds(
        IReadOnlyCollection<TestRunSnapshot> runs,
        IReadOnlyDictionary<int, int> userIdToTeamId = null)
    {
        if (runs == null || runs.Count <= 1)
        {
            return [];
        }

        var redundantIds = new List<int>();

        foreach (var partition in runs.GroupBy(run => ResolvePartitionKey(run.UserId, userIdToTeamId)))
        {
            EvaluatePartition(partition, redundantIds);
        }

        return redundantIds;
    }

    private static (bool IsTeam, int ScopeId) ResolvePartitionKey(
        int userId,
        IReadOnlyDictionary<int, int> userIdToTeamId)
    {
        if (userIdToTeamId != null && userIdToTeamId.TryGetValue(userId, out int teamId))
        {
            return (true, teamId);
        }

        return (false, userId);
    }

    private static void EvaluatePartition(
        IEnumerable<TestRunSnapshot> partition,
        List<int> redundantIds)
    {
        var cumulativeState = new Dictionary<int, bool>();
        bool isFirstRun = true;

        foreach (TestRunSnapshot run in partition
            .OrderBy(r => r.CreateDateTime)
            .ThenBy(r => r.TestRunId))
        {
            if (run.Results == null || run.Results.Count == 0)
            {
                if (isFirstRun)
                {
                    isFirstRun = false;
                }
                else
                {
                    redundantIds.Add(run.TestRunId);
                }

                continue;
            }

            bool stateChanged = ApplyResults(run.Results, cumulativeState);
            isFirstRun = false;

            if (!stateChanged)
            {
                redundantIds.Add(run.TestRunId);
            }
        }
    }

    private static bool ApplyResults(
        IReadOnlyList<TestResultSnapshot> results,
        Dictionary<int, bool> cumulativeState)
    {
        bool stateChanged = false;

        foreach (TestResultSnapshot result in results)
        {
            if (!cumulativeState.TryGetValue(result.TestId, out bool previousPassed) || previousPassed != result.Passed)
            {
                stateChanged = true;
                cumulativeState[result.TestId] = result.Passed;
            }
        }

        return stateChanged;
    }
}
