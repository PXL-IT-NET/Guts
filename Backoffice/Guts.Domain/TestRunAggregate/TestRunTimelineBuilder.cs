using System;
using System.Collections.Generic;
using System.Linq;

namespace Guts.Domain.TestRunAggregate;

public sealed record TestRunTimelinePoint(
    int TestRunId,
    int RunNumber,
    DateTime CreateDateTimeUtc,
    int PassedTestsCount,
    int FailedTestsCount,
    int TotalTestsCount,
    bool TestResultsChanged,
    bool SolutionFilesChanged,
    int TestResultsDelta);

public static class TestRunTimelineBuilder
{
    public static readonly TimeSpan SolutionFileCorrelationTolerance = TimeSpan.FromSeconds(2);

    public static IReadOnlyList<TestRunTimelinePoint> Build(
        IReadOnlyCollection<TestRunSnapshot> runs,
        IReadOnlyCollection<DateTime> solutionFileModifyDateTimesUtc,
        int totalTestsCount)
    {
        if (runs == null || runs.Count == 0)
        {
            return [];
        }

        List<TestRunSnapshot> orderedRuns = runs
            .OrderBy(r => NormalizeUtc(r.CreateDateTime))
            .ThenBy(r => r.TestRunId)
            .ToList();

        List<DateTime> orderedFileTimes = (solutionFileModifyDateTimesUtc ?? [])
            .Select(NormalizeUtc)
            .OrderBy(dt => dt)
            .ToList();

        var points = new List<TestRunTimelinePoint>(orderedRuns.Count);
        var cumulativeState = new Dictionary<int, bool>();
        int fileIndex = 0;
        int previousPassedCount = 0;
        int normalizedTotalTests = Math.Max(0, totalTestsCount);

        for (int i = 0; i < orderedRuns.Count; i++)
        {
            TestRunSnapshot run = orderedRuns[i];
            DateTime runTimeUtc = NormalizeUtc(run.CreateDateTime);
            DateTime correlationCutoff = runTimeUtc.Add(SolutionFileCorrelationTolerance);

            bool solutionFilesChanged = false;
            while (fileIndex < orderedFileTimes.Count && orderedFileTimes[fileIndex] <= correlationCutoff)
            {
                solutionFilesChanged = true;
                fileIndex++;
            }

            bool testResultsChanged = ApplyResults(run.Results, cumulativeState);
            int passedCount = cumulativeState.Values.Count(passed => passed);
            int failedCount = cumulativeState.Values.Count(passed => !passed);
            int effectiveTotal = Math.Max(normalizedTotalTests, cumulativeState.Count);
            int delta = i == 0 ? 0 : passedCount - previousPassedCount;
            previousPassedCount = passedCount;

            points.Add(new TestRunTimelinePoint(
                run.TestRunId,
                i + 1,
                runTimeUtc,
                passedCount,
                failedCount,
                effectiveTotal,
                testResultsChanged,
                solutionFilesChanged,
                delta));
        }

        return points;
    }

    private static bool ApplyResults(
        IReadOnlyList<TestResultSnapshot> results,
        Dictionary<int, bool> cumulativeState)
    {
        if (results == null || results.Count == 0)
        {
            return false;
        }

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

    private static DateTime NormalizeUtc(DateTime dt)
    {
        return dt.Kind switch
        {
            DateTimeKind.Utc => dt,
            DateTimeKind.Local => dt.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dt, DateTimeKind.Utc)
        };
    }
}
