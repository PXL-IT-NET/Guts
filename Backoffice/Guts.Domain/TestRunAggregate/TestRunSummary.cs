using System;
using System.Collections.Generic;

namespace Guts.Domain.TestRunAggregate;

public class TestRunSummary
{
    public DateTime? FirstRunDateTime { get; }
    public DateTime? LastRunDateTime { get; }
    public int NumberOfRuns { get; }

    public TestRunSummary(DateTime? firstRunDateTime, DateTime? lastRunDateTime, int numberOfRuns)
    {
        FirstRunDateTime = firstRunDateTime;
        LastRunDateTime = lastRunDateTime;
        NumberOfRuns = numberOfRuns;
    }

    public static TestRunSummary FromTestRuns(IReadOnlyList<TestRun> testRuns)
    {
        if (testRuns == null || testRuns.Count == 0)
        {
            return new TestRunSummary(null, null, 0);
        }

        var firstTestRun = testRuns[0];
        var lastTestRun = testRuns[^1];

        return new TestRunSummary(firstTestRun.CreateDateTime, lastTestRun.CreateDateTime, testRuns.Count);
    }
}

