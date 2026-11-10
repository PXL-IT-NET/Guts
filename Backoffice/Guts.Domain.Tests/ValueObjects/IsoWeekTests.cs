using System;
using System.Globalization;
using Guts.Common;
using Guts.Domain.ValueObjects;
using NUnit.Framework;

namespace Guts.Domain.Tests.ValueObjects;

[TestFixture]
public class IsoWeekTests
{
    [Test]
    public void From_ValidYearAndWeek_ShouldCreateIsoWeek()
    {
        var week = IsoWeek.From(2026, 42);

        Assert.That(week.Year, Is.EqualTo(2026));
        Assert.That(week.WeekNumber, Is.EqualTo(42));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void From_InvalidYear_ShouldThrowContractException(int invalidYear)
    {
        Assert.Throws<ContractException>(() => IsoWeek.From(invalidYear, 10));
    }

    [TestCase(0)]
    [TestCase(54)]
    [TestCase(-1)]
    public void From_InvalidWeekNumber_ShouldThrowContractException(int invalidWeek)
    {
        Assert.Throws<ContractException>(() => IsoWeek.From(2026, invalidWeek));
    }

    [Test]
    public void From_DateTime_ShouldDeriveCorrectIsoWeek()
    {
        // 2026-10-21 is Wednesday of Week 43 in 2026
        var date = new DateTime(2026, 10, 21, 14, 30, 0, DateTimeKind.Utc);
        var week = IsoWeek.From(date);

        Assert.That(week.Year, Is.EqualTo(2026));
        Assert.That(week.WeekNumber, Is.EqualTo(43));
    }

    [Test]
    public void StartOfWeek_ShouldBeMonday00Utc()
    {
        var week = IsoWeek.From(2026, 43);

        Assert.That(week.StartOfWeek, Is.EqualTo(new DateTime(2026, 10, 19, 0, 0, 0, DateTimeKind.Utc)));
        Assert.That(week.StartOfWeek.Kind, Is.EqualTo(DateTimeKind.Utc));
    }

    [Test]
    public void FridayCutoff_ShouldBeFriday235959Utc()
    {
        var week = IsoWeek.From(2026, 43);

        Assert.That(week.FridayCutoff, Is.EqualTo(new DateTime(2026, 10, 23, 23, 59, 59, DateTimeKind.Utc)));
    }

    [Test]
    public void EndOfWeek_ShouldBeSunday235959Utc()
    {
        var week = IsoWeek.From(2026, 43);

        Assert.That(week.EndOfWeek, Is.EqualTo(new DateTime(2026, 10, 25, 23, 59, 59, DateTimeKind.Utc)));
    }

    [Test]
    public void Previous_And_Next_ShouldTraverseChronologically()
    {
        var week43 = IsoWeek.From(2026, 43);
        var week42 = week43.Previous();
        var week44 = week43.Next();

        Assert.That(week42, Is.EqualTo(IsoWeek.From(2026, 42)));
        Assert.That(week44, Is.EqualTo(IsoWeek.From(2026, 44)));
    }

    [Test]
    public void Previous_AcrossYearBoundary_ShouldReturnLastWeekOfPreviousYear()
    {
        var week1 = IsoWeek.From(2026, 1);
        var prevWeek = week1.Previous();

        Assert.That(prevWeek.Year, Is.EqualTo(2025));
        Assert.That(prevWeek.WeekNumber, Is.EqualTo(ISOWeek.GetWeeksInYear(2025)));
    }

    [Test]
    public void Next_AcrossYearBoundary_ShouldReturnWeek1OfNextYear()
    {
        int weeksIn2025 = ISOWeek.GetWeeksInYear(2025);
        var lastWeek2025 = IsoWeek.From(2025, weeksIn2025);
        var nextWeek = lastWeek2025.Next();

        Assert.That(nextWeek, Is.EqualTo(IsoWeek.From(2026, 1)));
    }

    [Test]
    public void WeeksBetween_ShouldCalculateSignedDifference()
    {
        var week41 = IsoWeek.From(2026, 41);
        var week43 = IsoWeek.From(2026, 43);

        Assert.That(week41.WeeksBetween(week43), Is.EqualTo(2));
        Assert.That(week43.WeeksBetween(week41), Is.EqualTo(-2));
        Assert.That(week41.WeeksBetween(week41), Is.EqualTo(0));
    }

    [Test]
    public void Contains_WithinAndOutsideWeek_ShouldReturnExpectedResult()
    {
        var week = IsoWeek.From(2026, 43); // 2026-10-19 to 2026-10-25

        Assert.That(week.Contains(new DateTime(2026, 10, 19, 0, 0, 0, DateTimeKind.Utc)), Is.True);
        Assert.That(week.Contains(new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc)), Is.True);
        Assert.That(week.Contains(new DateTime(2026, 10, 25, 23, 59, 59, DateTimeKind.Utc)), Is.True);

        Assert.That(week.Contains(new DateTime(2026, 10, 18, 23, 59, 59, DateTimeKind.Utc)), Is.False);
        Assert.That(week.Contains(new DateTime(2026, 10, 26, 0, 0, 0, DateTimeKind.Utc)), Is.False);
    }

    [Test]
    public void ComparisonAndEquality_ShouldOrderProperly()
    {
        var w1 = IsoWeek.From(2025, 52);
        var w2 = IsoWeek.From(2026, 1);
        var w3 = IsoWeek.From(2026, 1);
        var w4 = IsoWeek.From(2026, 2);

        Assert.That(w2, Is.EqualTo(w3));
        Assert.That(w1 < w2, Is.True);
        Assert.That(w4 > w2, Is.True);
        Assert.That(w2 >= w3, Is.True);
        Assert.That(w2 <= w3, Is.True);
        Assert.That(w1.CompareTo(w2), Is.LessThan(0));
        Assert.That(w4.CompareTo(w2), Is.GreaterThan(0));
        Assert.That(w2.CompareTo(w3), Is.Zero);
    }
}
