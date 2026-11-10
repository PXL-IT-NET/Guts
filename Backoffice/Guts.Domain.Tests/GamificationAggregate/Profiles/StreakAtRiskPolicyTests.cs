using System;
using System.Collections.Generic;
using Guts.Common;
using Guts.Domain.GamificationAggregate.Profiles;
using Guts.Domain.ValueObjects;
using NUnit.Framework;

namespace Guts.Domain.Tests.GamificationAggregate.Profiles;

[TestFixture]
public class StreakAtRiskPolicyTests
{
    private IsoWeek _currentWeek;
    private DateTime _mondayCurrentWeek;

    [SetUp]
    public void SetUp()
    {
        _mondayCurrentWeek = new DateTime(2026, 10, 19, 0, 0, 0, DateTimeKind.Utc); // Monday Week 43
        _currentWeek = IsoWeek.From(_mondayCurrentWeek);
    }

    [Test]
    public void IsStreakAtRisk_CurrentStreakIsZero_ShouldReturnFalse()
    {
        var profile = new StudentCourseProfile(1, 10, 20);
        var allCourseTestIds = new HashSet<int> { 101, 102, 103 };

        bool result = StreakAtRiskPolicy.IsStreakAtRisk(profile, _currentWeek, allCourseTestIds);

        Assert.That(result, Is.False);
    }

    [Test]
    public void IsStreakAtRisk_ActiveStreakAndHasEarnedXpThisWeek_ShouldReturnFalse()
    {
        var profile = new StudentCourseProfile(1, 10, 20);
        var previousWeekMonday = _mondayCurrentWeek.AddDays(-7);

        // Active last week -> streak = 1
        profile.AwardPassedTestXp(101, previousWeekMonday);
        Assert.That(profile.WeeklyStreak.CurrentStreak, Is.EqualTo(1));

        // Active this week -> streak = 2
        profile.AwardPassedTestXp(102, _mondayCurrentWeek.AddDays(1));
        Assert.That(profile.WeeklyStreak.CurrentStreak, Is.EqualTo(2));

        var allCourseTestIds = new HashSet<int> { 101, 102, 103 };

        bool result = StreakAtRiskPolicy.IsStreakAtRisk(profile, _currentWeek, allCourseTestIds);

        Assert.That(result, Is.False);
    }

    [Test]
    public void IsStreakAtRisk_ActiveStreakAndNoXpThisWeekAndUnpassedTestsRemain_ShouldReturnTrue()
    {
        var profile = new StudentCourseProfile(1, 10, 20);
        var previousWeekMonday = _mondayCurrentWeek.AddDays(-7);

        // Active last week -> streak = 1
        profile.AwardPassedTestXp(101, previousWeekMonday);
        Assert.That(profile.WeeklyStreak.CurrentStreak, Is.EqualTo(1));

        var allCourseTestIds = new HashSet<int> { 101, 102, 103 };

        bool result = StreakAtRiskPolicy.IsStreakAtRisk(profile, _currentWeek, allCourseTestIds);

        Assert.That(result, Is.True);
    }

    [Test]
    public void IsStreakAtRisk_ActiveStreakAndNoXpThisWeekButAllAvailableCourseTestsPassed_ShouldReturnFalse()
    {
        var profile = new StudentCourseProfile(1, 10, 20);
        var previousWeekMonday = _mondayCurrentWeek.AddDays(-7);

        // Student passed all 2 tests in course last week
        profile.AwardPassedTestXp(101, previousWeekMonday);
        profile.AwardPassedTestXp(102, previousWeekMonday);
        Assert.That(profile.WeeklyStreak.CurrentStreak, Is.EqualTo(1));

        var allCourseTestIds = new HashSet<int> { 101, 102 };

        bool result = StreakAtRiskPolicy.IsStreakAtRisk(profile, _currentWeek, allCourseTestIds);

        // Streak is safe because all available exercises in the course are completed
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsStreakAtRisk_NullArguments_ShouldThrowContractException()
    {
        var profile = new StudentCourseProfile(1, 10, 20);
        var allCourseTestIds = new HashSet<int> { 101 };

        Assert.Throws<ContractException>(() => StreakAtRiskPolicy.IsStreakAtRisk(null!, _currentWeek, allCourseTestIds));
        Assert.Throws<ContractException>(() => StreakAtRiskPolicy.IsStreakAtRisk(profile, null!, allCourseTestIds));
        Assert.Throws<ContractException>(() => StreakAtRiskPolicy.IsStreakAtRisk(profile, _currentWeek, null!));
    }
}

