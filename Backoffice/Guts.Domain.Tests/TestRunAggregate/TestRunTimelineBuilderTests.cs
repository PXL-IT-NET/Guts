using System;
using Guts.Domain.TestRunAggregate;
using NUnit.Framework;

namespace Guts.Domain.Tests.TestRunAggregate;

[TestFixture]
public class TestRunTimelineBuilderTests
{
    private static readonly DateTime BaseTime = new(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);

    [Test]
    public void Build_WhenRunsNullOrEmpty_ShouldReturnEmpty()
    {
        Assert.That(TestRunTimelineBuilder.Build(null, null, 5), Is.Empty);
        Assert.That(TestRunTimelineBuilder.Build([], [BaseTime], 5), Is.Empty);
    }

    [Test]
    public void Build_ShouldSortChronologicallyComputeCumulativeCountsDeltasAndCorrelateSolutionFiles()
    {
        var runs = new[]
        {
            // Out of order intentionally
            new TestRunSnapshot(4, 1, BaseTime.AddMinutes(15), [new TestResultSnapshot(2, false)]), // Regression -> 1 passed (-1), code changed
            new TestRunSnapshot(1, 1, BaseTime, [new TestResultSnapshot(1, true), new TestResultSnapshot(2, false)]), // First run: 1 passed, code changed
            new TestRunSnapshot(3, 1, BaseTime.AddMinutes(10), [new TestResultSnapshot(2, true)]), // Partial run flips T2 green -> 2 passed (+1), no code change
            new TestRunSnapshot(2, 1, BaseTime.AddMinutes(5), [new TestResultSnapshot(2, false)]), // Partial run unchanged, code changed only
            new TestRunSnapshot(5, 1, BaseTime.AddMinutes(20), [new TestResultSnapshot(2, false)]) // Unchanged tests & unchanged code
        };

        var fileModifyTimes = new[]
        {
            BaseTime.AddMilliseconds(-200), // Attributed to run 1 (right before run 1)
            BaseTime.AddMilliseconds(-50),  // Second file in run 1 window -> still run 1
            BaseTime.AddMinutes(5).AddSeconds(1), // Within 2s tolerance of run 2 -> attributed to run 2 (code-only change!)
            BaseTime.AddMinutes(14).AddSeconds(59), // Attributed to run 4 (regression + code change)
            BaseTime.AddMinutes(30) // After all runs -> safely ignored
        };

        var points = TestRunTimelineBuilder.Build(runs, fileModifyTimes, totalTestsCount: 3);

        Assert.That(points, Has.Count.EqualTo(5));

        // Run #1 (id=1): first run, 1/3 passed, delta 0, tests changed + code changed
        Assert.That(points[0].TestRunId, Is.EqualTo(1));
        Assert.That(points[0].RunNumber, Is.EqualTo(1));
        Assert.That(points[0].PassedTestsCount, Is.EqualTo(1));
        Assert.That(points[0].FailedTestsCount, Is.EqualTo(1));
        Assert.That(points[0].TotalTestsCount, Is.EqualTo(3));
        Assert.That(points[0].TestResultsChanged, Is.True);
        Assert.That(points[0].SolutionFilesChanged, Is.True);
        Assert.That(points[0].TestResultsDelta, Is.EqualTo(0));

        // Run #2 (id=2): unchanged tests, code changed only
        Assert.That(points[1].TestRunId, Is.EqualTo(2));
        Assert.That(points[1].RunNumber, Is.EqualTo(2));
        Assert.That(points[1].PassedTestsCount, Is.EqualTo(1));
        Assert.That(points[1].TestResultsChanged, Is.False);
        Assert.That(points[1].SolutionFilesChanged, Is.True);
        Assert.That(points[1].TestResultsDelta, Is.EqualTo(0));

        // Run #3 (id=3): partial run flips T2 green -> 2/3 passed, +1 delta, no code change
        Assert.That(points[2].TestRunId, Is.EqualTo(3));
        Assert.That(points[2].RunNumber, Is.EqualTo(3));
        Assert.That(points[2].PassedTestsCount, Is.EqualTo(2));
        Assert.That(points[2].FailedTestsCount, Is.EqualTo(0));
        Assert.That(points[2].TestResultsChanged, Is.True);
        Assert.That(points[2].SolutionFilesChanged, Is.False);
        Assert.That(points[2].TestResultsDelta, Is.EqualTo(1));

        // Run #4 (id=4): regression T2 -> red -> 1/3 passed, -1 delta, code changed
        Assert.That(points[3].TestRunId, Is.EqualTo(4));
        Assert.That(points[3].RunNumber, Is.EqualTo(4));
        Assert.That(points[3].PassedTestsCount, Is.EqualTo(1));
        Assert.That(points[3].FailedTestsCount, Is.EqualTo(1));
        Assert.That(points[3].TestResultsChanged, Is.True);
        Assert.That(points[3].SolutionFilesChanged, Is.True);
        Assert.That(points[3].TestResultsDelta, Is.EqualTo(-1));

        // Run #5 (id=5): completely unchanged
        Assert.That(points[4].TestRunId, Is.EqualTo(5));
        Assert.That(points[4].RunNumber, Is.EqualTo(5));
        Assert.That(points[4].PassedTestsCount, Is.EqualTo(1));
        Assert.That(points[4].TestResultsChanged, Is.False);
        Assert.That(points[4].SolutionFilesChanged, Is.False);
        Assert.That(points[4].TestResultsDelta, Is.EqualTo(0));
    }

    [Test]
    public void Build_WhenRunHasEmptyResultsAndNonUtcTimestamps_ShouldNormalizeAndHandleSafely()
    {
        var unspecifiedTime = new DateTime(2026, 11, 20, 10, 0, 0, DateTimeKind.Unspecified);
        var localTime = DateTime.SpecifyKind(BaseTime.AddHours(15), DateTimeKind.Local);

        var runs = new[]
        {
            new TestRunSnapshot(10, 1, unspecifiedTime, []),
            new TestRunSnapshot(11, 1, localTime, [new TestResultSnapshot(1, true), new TestResultSnapshot(2, true)])
        };

        var points = TestRunTimelineBuilder.Build(runs, null, totalTestsCount: 0);

        Assert.That(points, Has.Count.EqualTo(2));
        Assert.That(points[0].CreateDateTimeUtc.Kind, Is.EqualTo(DateTimeKind.Utc));
        Assert.That(points[0].TestResultsChanged, Is.False);
        Assert.That(points[0].PassedTestsCount, Is.EqualTo(0));
        Assert.That(points[1].CreateDateTimeUtc.Kind, Is.EqualTo(DateTimeKind.Utc));
        Assert.That(points[1].PassedTestsCount, Is.EqualTo(2));
        Assert.That(points[1].TotalTestsCount, Is.EqualTo(2)); // floored at cumulativeState.Count
    }
}
