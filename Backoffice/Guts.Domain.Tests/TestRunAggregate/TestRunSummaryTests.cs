using System;
using System.Collections.Generic;
using Guts.Domain.TestRunAggregate;
using NUnit.Framework;

namespace Guts.Domain.Tests.TestRunAggregate;

[TestFixture]
public class TestRunSummaryTests
{
    [Test]
    public void FromTestRuns_NullOrEmptyList_ReturnsDefaultSummary()
    {
        var summaryNull = TestRunSummary.FromTestRuns(null);
        Assert.That(summaryNull.FirstRunDateTime, Is.Null);
        Assert.That(summaryNull.LastRunDateTime, Is.Null);
        Assert.That(summaryNull.NumberOfRuns, Is.EqualTo(0));

        var summaryEmpty = TestRunSummary.FromTestRuns([]);
        Assert.That(summaryEmpty.FirstRunDateTime, Is.Null);
        Assert.That(summaryEmpty.LastRunDateTime, Is.Null);
        Assert.That(summaryEmpty.NumberOfRuns, Is.EqualTo(0));
    }

    [Test]
    public void FromTestRuns_WithRuns_ExtractsFirstAndLastDateTimeAndCount()
    {
        var dt1 = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var dt2 = new DateTime(2026, 1, 2, 11, 0, 0, DateTimeKind.Utc);
        var dt3 = new DateTime(2026, 1, 3, 12, 0, 0, DateTimeKind.Utc);

        var runs = new List<TestRun>
        {
            new() { Id = 1, CreateDateTime = dt1 },
            new() { Id = 2, CreateDateTime = dt2 },
            new() { Id = 3, CreateDateTime = dt3 }
        };

        var summary = TestRunSummary.FromTestRuns(runs);

        Assert.That(summary.FirstRunDateTime, Is.EqualTo(dt1));
        Assert.That(summary.LastRunDateTime, Is.EqualTo(dt3));
        Assert.That(summary.NumberOfRuns, Is.EqualTo(3));
    }
}

