using System;
using System.Globalization;
using Guts.Common;
using Guts.Domain.GamificationAggregate;
using NUnit.Framework;

namespace Guts.Domain.Tests.GamificationAggregate;

[TestFixture]
public class WeeklyStreakTests
{
    [Test]
    public void DefaultConstructor_ShouldInitializeWithZeroStreak()
    {
        var streak = new WeeklyStreak();
        Assert.That(streak.CurrentStreak, Is.EqualTo(0));
        Assert.That(streak.BestStreak, Is.EqualTo(0));
        Assert.That(streak.LastActiveYear, Is.EqualTo(0));
        Assert.That(streak.LastActiveWeek, Is.EqualTo(0));
    }

    [Test]
    public void ParameterizedConstructor_ValidValues_ShouldSetProperties()
    {
        var streak = new WeeklyStreak(3, 5, 2026, 42);
        Assert.That(streak.CurrentStreak, Is.EqualTo(3));
        Assert.That(streak.BestStreak, Is.EqualTo(5));
        Assert.That(streak.LastActiveYear, Is.EqualTo(2026));
        Assert.That(streak.LastActiveWeek, Is.EqualTo(42));
    }

    [Test]
    public void ParameterizedConstructor_BestStreakLessThanCurrent_ShouldThrowContractException()
    {
        Assert.Throws<ContractException>(() => new WeeklyStreak(5, 3, 2026, 42));
    }

    [Test]
    public void RecordActivity_FirstTime_ShouldStartStreakAtOne()
    {
        var streak = new WeeklyStreak();
        var now = new DateTime(2026, 10, 21); // Wednesday
        var updated = streak.RecordActivity(now);

        Assert.That(updated.CurrentStreak, Is.EqualTo(1));
        Assert.That(updated.BestStreak, Is.EqualTo(1));
        Assert.That(updated.LastActiveYear, Is.EqualTo(ISOWeek.GetYear(now)));
        Assert.That(updated.LastActiveWeek, Is.EqualTo(ISOWeek.GetWeekOfYear(now)));
    }

    [Test]
    public void RecordActivity_SameWeek_ShouldNotIncrementStreak()
    {
        var now = new DateTime(2026, 10, 21); // Wednesday
        var streak = new WeeklyStreak().RecordActivity(now);
        var nextDay = now.AddDays(1); // Thursday of same week
        var updated = streak.RecordActivity(nextDay);

        Assert.That(updated.CurrentStreak, Is.EqualTo(1));
        Assert.That(updated.BestStreak, Is.EqualTo(1));
    }

    [Test]
    public void RecordActivity_ConsecutiveWeek_ShouldIncrementStreak()
    {
        var week1 = new DateTime(2026, 10, 19); // Monday Week 43
        var week2 = week1.AddDays(7); // Monday Week 44

        var streak = new WeeklyStreak().RecordActivity(week1);
        var updated = streak.RecordActivity(week2);

        Assert.That(updated.CurrentStreak, Is.EqualTo(2));
        Assert.That(updated.BestStreak, Is.EqualTo(2));
        Assert.That(updated.LastActiveWeek, Is.EqualTo(ISOWeek.GetWeekOfYear(week2)));
    }

    [Test]
    public void RecordActivity_SkippedWeek_ShouldResetStreakToOne()
    {
        var week1 = new DateTime(2026, 10, 19); // Monday Week 43
        var week3 = week1.AddDays(14); // Monday Week 45 (skipped Week 44)

        var streak = new WeeklyStreak().RecordActivity(week1);
        var updated = streak.RecordActivity(week3);

        Assert.That(updated.CurrentStreak, Is.EqualTo(1));
        Assert.That(updated.BestStreak, Is.EqualTo(1));
    }

    [Test]
    public void IsActiveInWeek_MatchingWeek_ShouldReturnTrue()
    {
        var now = new DateTime(2026, 10, 21);
        var streak = new WeeklyStreak().RecordActivity(now);

        Assert.That(streak.IsActiveInWeek(now), Is.True);
        Assert.That(streak.IsActiveInWeek(now.AddDays(10)), Is.False);
    }

    [Test]
    public void RecordActivity_PastWeek_ShouldReturnUnchangedStreak()
    {
        var week2 = new DateTime(2026, 10, 26); // Monday Week 44
        var week1 = week2.AddDays(-7); // Monday Week 43

        var streak = new WeeklyStreak().RecordActivity(week2);
        var updated = streak.RecordActivity(week1);

        Assert.That(updated, Is.EqualTo(streak));
        Assert.That(updated.CurrentStreak, Is.EqualTo(1));
        Assert.That(updated.LastActiveWeek, Is.EqualTo(ISOWeek.GetWeekOfYear(week2)));
    }

    [Test]
    public void Equality_SameAndDifferentValues_ShouldCompareCorrectly()
    {
        var s1 = new WeeklyStreak(2, 4, 2026, 43);
        var s2 = new WeeklyStreak(2, 4, 2026, 43);
        var s3 = new WeeklyStreak(1, 4, 2026, 43);

        Assert.That(s1, Is.EqualTo(s2));
        Assert.That(s1.GetHashCode(), Is.EqualTo(s2.GetHashCode()));
        Assert.That(s1, Is.Not.EqualTo(s3));
    }

    [Test]
    public void NeedsEvaluation_WhenZeroStreakOrActiveCurrentOrPreviousWeek_ShouldReturnFalse()
    {
        var emptyStreak = new WeeklyStreak();
        var week43 = new DateTime(2026, 10, 19); // Monday Week 43
        var week44 = week43.AddDays(7); // Monday Week 44

        Assert.That(emptyStreak.NeedsEvaluation(week44), Is.False);

        var activeWeek43 = emptyStreak.RecordActivity(week43);
        Assert.That(activeWeek43.NeedsEvaluation(week43), Is.False); // Same week (weeksDiff == 0)
        Assert.That(activeWeek43.NeedsEvaluation(week44), Is.False); // Next week (weeksDiff == 1)
    }

    [Test]
    public void NeedsEvaluation_WhenActiveStreakMissedLastWeek_ShouldReturnTrue()
    {
        var week42 = new DateTime(2026, 10, 12); // Monday Week 42
        var week44 = week42.AddDays(14); // Monday Week 44 (missed Week 43)

        var streak = new WeeklyStreak(3, 5, ISOWeek.GetYear(week42), ISOWeek.GetWeekOfYear(week42));

        Assert.That(streak.NeedsEvaluation(week44), Is.True);
    }

    [Test]
    public void EvaluateStreak_WhenDoesNotNeedEvaluation_ShouldReturnSameInstance()
    {
        var week43 = new DateTime(2026, 10, 19); // Week 43
        var week44 = week43.AddDays(7); // Week 44
        var streak = new WeeklyStreak(3, 5, ISOWeek.GetYear(week43), ISOWeek.GetWeekOfYear(week43));

        var evaluated = streak.EvaluateStreak(week44, hasPassedAllAvailableTests: false);

        Assert.That(evaluated, Is.SameAs(streak));
    }

    [Test]
    public void EvaluateStreak_WhenMissedLastWeekAndNotAllTestsPassed_ShouldResetCurrentStreakToZeroAndKeepBestStreak()
    {
        var week42 = new DateTime(2026, 10, 12); // Week 42
        var week44 = week42.AddDays(14); // Week 44 (missed Week 43)
        var streak = new WeeklyStreak(3, 5, ISOWeek.GetYear(week42), ISOWeek.GetWeekOfYear(week42));

        var evaluated = streak.EvaluateStreak(week44, hasPassedAllAvailableTests: false);

        Assert.That(evaluated.CurrentStreak, Is.Zero);
        Assert.That(evaluated.BestStreak, Is.EqualTo(5));
        Assert.That(evaluated.LastActiveYear, Is.EqualTo(ISOWeek.GetYear(week42)));
        Assert.That(evaluated.LastActiveWeek, Is.EqualTo(ISOWeek.GetWeekOfYear(week42)));
        Assert.That(evaluated.NeedsEvaluation(week44), Is.False);
    }

    [Test]
    public void EvaluateStreak_WhenMissedLastWeekAndAllAvailableTestsPassed_ShouldKeepStreakAndAdvanceLastActiveWeek()
    {
        var week42 = new DateTime(2026, 10, 12); // Week 42
        var week43 = week42.AddDays(7); // Week 43
        var week44 = week42.AddDays(14); // Week 44 (missed Week 43)
        var streak = new WeeklyStreak(3, 5, ISOWeek.GetYear(week42), ISOWeek.GetWeekOfYear(week42));

        var evaluated = streak.EvaluateStreak(week44, hasPassedAllAvailableTests: true);

        Assert.That(evaluated.CurrentStreak, Is.EqualTo(3));
        Assert.That(evaluated.BestStreak, Is.EqualTo(5));
        Assert.That(evaluated.LastActiveYear, Is.EqualTo(ISOWeek.GetYear(week43)));
        Assert.That(evaluated.LastActiveWeek, Is.EqualTo(ISOWeek.GetWeekOfYear(week43)));
        Assert.That(evaluated.NeedsEvaluation(week44), Is.False);

        // Subsequent activity in Week 44 should increment from 3 to 4
        var afterNewTestInWeek44 = evaluated.RecordActivity(week44);
        Assert.That(afterNewTestInWeek44.CurrentStreak, Is.EqualTo(4));
    }
}