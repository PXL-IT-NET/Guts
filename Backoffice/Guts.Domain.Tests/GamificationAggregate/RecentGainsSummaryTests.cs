using System;
using System.Collections.Generic;
using Guts.Domain.GamificationAggregate;
using Guts.Domain.GamificationAggregate.Profiles;
using NUnit.Framework;

namespace Guts.Domain.Tests.GamificationAggregate;

[TestFixture]
public class RecentGainsSummaryTests
{
    [Test]
    public void Constructor_NullOrEmptyEvents_InitializesZeroXpAndEmptyGains()
    {
        var summaryNull = new RecentGainsSummary(null, null);
        Assert.That(summaryNull.TotalXpGained, Is.EqualTo(0));
        Assert.That(summaryNull.PreviousVisitDate, Is.Null);
        Assert.That(summaryNull.Gains, Is.Empty);

        var visitDate = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);
        var summaryEmpty = new RecentGainsSummary(visitDate, []);
        Assert.That(summaryEmpty.TotalXpGained, Is.EqualTo(0));
        Assert.That(summaryEmpty.PreviousVisitDate, Is.EqualTo(visitDate));
        Assert.That(summaryEmpty.Gains, Is.Empty);
    }

    [Test]
    public void Constructor_PassedTestEvents_AggregatesSingularAndPluralCorrectly()
    {
        var t1 = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);
        var t2 = new DateTime(2026, 3, 1, 10, 5, 0, DateTimeKind.Utc);

        // 1 test: singular
        var singleTestEvent = new List<GamificationEvent>
        {
            new() { EventType = GamificationEventType.PassedTest, XpEarned = 10, CreatedAt = t1, TargetId = 101 }
        };
        var summarySingle = new RecentGainsSummary(null, singleTestEvent);
        Assert.That(summarySingle.TotalXpGained, Is.EqualTo(10));
        Assert.That(summarySingle.Gains, Has.Count.EqualTo(1));
        Assert.That(summarySingle.Gains[0].Description, Is.EqualTo("Made 1 test green"));
        Assert.That(summarySingle.Gains[0].XpEarned, Is.EqualTo(10));

        // 2 tests: plural
        var multiTestEvents = new List<GamificationEvent>
        {
            new() { EventType = GamificationEventType.PassedTest, XpEarned = 10, CreatedAt = t1, TargetId = 101 },
            new() { EventType = GamificationEventType.PassedTest, XpEarned = 15, CreatedAt = t2, TargetId = 102 }
        };
        var summaryMulti = new RecentGainsSummary(null, multiTestEvents);
        Assert.That(summaryMulti.TotalXpGained, Is.EqualTo(25));
        Assert.That(summaryMulti.Gains, Has.Count.EqualTo(1));
        Assert.That(summaryMulti.Gains[0].Description, Is.EqualTo("Made 2 tests green"));
        Assert.That(summaryMulti.Gains[0].XpEarned, Is.EqualTo(25));
        Assert.That(summaryMulti.Gains[0].CreatedAt, Is.EqualTo(t2));
    }

    [Test]
    public void Constructor_OvertakeEvents_AggregatesAndIgnoresZeroXpOvertakes()
    {
        var t1 = new DateTime(2026, 3, 1, 11, 0, 0, DateTimeKind.Utc);
        var t2 = new DateTime(2026, 3, 1, 11, 1, 0, DateTimeKind.Utc);

        // Overtake with 0 XP should not be included
        var zeroXpOvertake = new List<GamificationEvent>
        {
            new() { EventType = GamificationEventType.HallOfFameOvertake, XpEarned = 0, CreatedAt = t1, TargetId = 201 }
        };
        var summaryZero = new RecentGainsSummary(null, zeroXpOvertake);
        Assert.That(summaryZero.Gains, Is.Empty);

        // 2 overtakes with XP
        var multiOvertakes = new List<GamificationEvent>
        {
            new() { EventType = GamificationEventType.HallOfFameOvertake, XpEarned = 5, CreatedAt = t1, TargetId = 201 },
            new() { EventType = GamificationEventType.HallOfFameOvertake, XpEarned = 5, CreatedAt = t2, TargetId = 202 }
        };
        var summaryMulti = new RecentGainsSummary(null, multiOvertakes);
        Assert.That(summaryMulti.Gains, Has.Count.EqualTo(1));
        Assert.That(summaryMulti.Gains[0].Description, Is.EqualTo("Overtake bonus (surpassed 2 students in Hall of Fame)"));
        Assert.That(summaryMulti.Gains[0].XpEarned, Is.EqualTo(10));
    }

    [Test]
    public void Constructor_MixedBonuses_MapsDescriptionsAndOrdersDescending()
    {
        var t1 = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);
        var t2 = new DateTime(2026, 3, 1, 11, 0, 0, DateTimeKind.Utc);
        var t3 = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

        var events = new List<GamificationEvent>
        {
            new() { EventType = GamificationEventType.PassedTest, XpEarned = 10, CreatedAt = t1, TargetId = 1 },
            new() { EventType = GamificationEventType.AssignmentCompleted, XpEarned = 50, CreatedAt = t3, TargetId = 2 },
            new() { EventType = GamificationEventType.ChapterCompleted, XpEarned = 100, CreatedAt = t2, TargetId = 3 }
        };

        var summary = new RecentGainsSummary(null, events);

        Assert.That(summary.TotalXpGained, Is.EqualTo(160));
        Assert.That(summary.Gains, Has.Count.EqualTo(3));

        // Order: t3, then t2, then t1
        Assert.That(summary.Gains[0].CreatedAt, Is.EqualTo(t3));
        Assert.That(summary.Gains[0].Description, Is.EqualTo("Completed assignment bonus"));
        Assert.That(summary.Gains[0].XpEarned, Is.EqualTo(50));

        Assert.That(summary.Gains[1].CreatedAt, Is.EqualTo(t2));
        Assert.That(summary.Gains[1].Description, Is.EqualTo("Completed chapter bonus"));
        Assert.That(summary.Gains[1].XpEarned, Is.EqualTo(100));

        Assert.That(summary.Gains[2].CreatedAt, Is.EqualTo(t1));
        Assert.That(summary.Gains[2].Description, Is.EqualTo("Made 1 test green"));
        Assert.That(summary.Gains[2].XpEarned, Is.EqualTo(10));
    }
}
