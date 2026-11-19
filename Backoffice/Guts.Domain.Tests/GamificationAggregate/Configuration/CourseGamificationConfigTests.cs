using Guts.Common;
using Guts.Domain.GamificationAggregate.Configuration;
using NUnit.Framework;

namespace Guts.Domain.Tests.GamificationAggregate.Configuration;

[TestFixture]
public class CourseGamificationConfigTests
{
    [Test]
    public void Constructor_ValidCourseId_ShouldDisableGamificationByDefault()
    {
        var config = new CourseGamificationConfig(12);

        Assert.That(config.CourseId, Is.EqualTo(12));
        Assert.That(config.IsGamificationEnabled, Is.False);
        Assert.That(config.IsWeeklyStreakEnabled, Is.True);
        Assert.That(config.IsLeaderboardEnabled, Is.True);
        Assert.That(config.IsClassCompetitionEnabled, Is.True);
        Assert.That(config.IsTeamSynergyEnabled, Is.True);
        Assert.That(config.IsBadgesEnabled, Is.True);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Constructor_InvalidCourseId_ShouldThrowContractException(int invalidCourseId)
    {
        Assert.Throws<ContractException>(() => new CourseGamificationConfig(invalidCourseId));
    }

    [Test]
    public void UpdateSettings_ShouldUpdateAllFeatureFlags()
    {
        var config = new CourseGamificationConfig(10);

        config.UpdateSettings(
            isGamificationEnabled: true,
            isWeeklyStreakEnabled: false,
            isLeaderboardEnabled: true,
            isClassCompetitionEnabled: false,
            isTeamSynergyEnabled: true,
            isBadgesEnabled: false);

        Assert.That(config.IsGamificationEnabled, Is.True);
        Assert.That(config.IsWeeklyStreakEnabled, Is.False);
        Assert.That(config.IsLeaderboardEnabled, Is.True);
        Assert.That(config.IsClassCompetitionEnabled, Is.False);
        Assert.That(config.IsTeamSynergyEnabled, Is.True);
        Assert.That(config.IsBadgesEnabled, Is.False);
    }

    [Test]
    public void CapabilityProperties_WhenGamificationDisabled_ShouldAllReturnFalse()
    {
        var config = new CourseGamificationConfig(10);

        Assert.That(config.IsWeeklyStreakActive, Is.False);
        Assert.That(config.IsHallOfFameActive, Is.False);
        Assert.That(config.IsWeeklySprintActive, Is.False);
        Assert.That(config.CanAwardWeeklySprintBadges, Is.False);
        Assert.That(config.IsClassCompetitionActive, Is.False);
        Assert.That(config.IsTeamSynergyActive, Is.False);
    }

    [Test]
    public void CapabilityProperties_WhenGamificationEnabled_ShouldReflectSubFeatureFlags()
    {
        var config = new CourseGamificationConfig(10);
        config.UpdateSettings(
            isGamificationEnabled: true,
            isWeeklyStreakEnabled: true,
            isLeaderboardEnabled: true,
            isClassCompetitionEnabled: true,
            isTeamSynergyEnabled: true,
            isBadgesEnabled: true);

        Assert.That(config.IsWeeklyStreakActive, Is.True);
        Assert.That(config.IsHallOfFameActive, Is.True);
        Assert.That(config.IsWeeklySprintActive, Is.True);
        Assert.That(config.CanAwardWeeklySprintBadges, Is.True);
        Assert.That(config.IsClassCompetitionActive, Is.True);
        Assert.That(config.IsTeamSynergyActive, Is.True);

        config.UpdateSettings(
            isGamificationEnabled: true,
            isWeeklyStreakEnabled: true,
            isLeaderboardEnabled: true,
            isClassCompetitionEnabled: false,
            isTeamSynergyEnabled: false,
            isBadgesEnabled: false);

        Assert.That(config.IsWeeklyStreakActive, Is.True);
        Assert.That(config.IsHallOfFameActive, Is.True);
        Assert.That(config.IsWeeklySprintActive, Is.True);
        Assert.That(config.CanAwardWeeklySprintBadges, Is.False);
        Assert.That(config.IsClassCompetitionActive, Is.False);
        Assert.That(config.IsTeamSynergyActive, Is.False);
    }
}