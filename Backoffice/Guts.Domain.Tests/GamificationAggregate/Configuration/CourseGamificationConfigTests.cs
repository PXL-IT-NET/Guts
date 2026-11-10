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
}