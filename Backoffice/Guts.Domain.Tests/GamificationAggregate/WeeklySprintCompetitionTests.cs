using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Common;
using Guts.Domain.GamificationAggregate;
using NUnit.Framework;

namespace Guts.Domain.Tests.GamificationAggregate;

[TestFixture]
public class WeeklySprintCompetitionTests
{
    [Test]
    public void DetermineStandings_ShouldFilterZeroScores_AndOrderByScoreDescThenGreenTestsDesc()
    {
        var startOfWeek = new DateTime(2026, 10, 19, 0, 0, 0, DateTimeKind.Utc);
        var midWeek = new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);
        var cutoff = new DateTime(2026, 10, 23, 23, 59, 59, DateTimeKind.Utc);

        // s1: 60 XP (1 test + 1 assignment) -> 1 green test
        var s1 = new StudentCourseProfile(1, 10, 1);
        s1.AwardPassedTestXp(101, midWeek);
        s1.AwardAssignmentBonus(201, midWeek);

        // s2: 40 XP (4 green tests)
        var s2 = new StudentCourseProfile(2, 10, 1);
        s2.AwardPassedTestXp(102, midWeek);
        s2.AwardPassedTestXp(103, midWeek);
        s2.AwardPassedTestXp(104, midWeek);
        s2.AwardPassedTestXp(105, midWeek);

        // s3: 10 XP (1 green test)
        var s3 = new StudentCourseProfile(3, 10, 1);
        s3.AwardPassedTestXp(106, midWeek);

        // s4: Zero XP this week (event was 2 weeks ago)
        var s4 = new StudentCourseProfile(4, 10, 1);
        s4.AwardPassedTestXp(107, startOfWeek.AddDays(-14));

        var standings = WeeklySprintCompetition.DetermineStandings(new[] { s1, s2, s3, s4 }, startOfWeek, cutoff);

        Assert.That(standings.Count, Is.EqualTo(3)); // s4 excluded because Score == 0

        // 1st: s1 (60 XP)
        Assert.That(standings[0].Profile.UserId, Is.EqualTo(1));
        Assert.That(standings[0].Rank, Is.EqualTo(1));
        Assert.That(standings[0].Score, Is.EqualTo(60));
        Assert.That(standings[0].GreenTestsCount, Is.EqualTo(1));

        // 2nd: s2 (40 XP, 4 green tests)
        Assert.That(standings[1].Profile.UserId, Is.EqualTo(2));
        Assert.That(standings[1].Rank, Is.EqualTo(2));
        Assert.That(standings[1].Score, Is.EqualTo(40));
        Assert.That(standings[1].GreenTestsCount, Is.EqualTo(4));

        // 3rd: s3 (10 XP, 1 green test)
        Assert.That(standings[2].Profile.UserId, Is.EqualTo(3));
        Assert.That(standings[2].Rank, Is.EqualTo(3));
        Assert.That(standings[2].Score, Is.EqualTo(10));
        Assert.That(standings[2].GreenTestsCount, Is.EqualTo(1));
    }

    [Test]
    public void DetermineStandings_TieBreaking_ShouldRankHigherGreenTestsFirst()
    {
        var startOfWeek = new DateTime(2026, 10, 19, 0, 0, 0, DateTimeKind.Utc);
        var midWeek = new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);
        var cutoff = new DateTime(2026, 10, 23, 23, 59, 59, DateTimeKind.Utc);

        var s1 = new StudentCourseProfile(1, 10, 1);
        s1.AwardPassedTestXp(1, midWeek);
        s1.AwardPassedTestXp(2, midWeek);
        s1.AwardPassedTestXp(3, midWeek);
        s1.AwardPassedTestXp(4, midWeek);
        s1.AwardPassedTestXp(5, midWeek); // 50 XP, 5 green tests

        var s2 = new StudentCourseProfile(2, 10, 1);
        s2.AwardAssignmentBonus(100, midWeek); // 50 XP, 0 green tests

        var standings = WeeklySprintCompetition.DetermineStandings(new[] { s2, s1 }, startOfWeek, cutoff);

        Assert.That(standings[0].Profile.UserId, Is.EqualTo(1));
        Assert.That(standings[1].Profile.UserId, Is.EqualTo(2));
    }

    [Test]
    public void AwardBadges_FullStandings_ShouldEncapsulateTopFiveAndAwardHierarchicalBadges()
    {
        var cutoff = new DateTime(2026, 10, 23, 23, 59, 59, DateTimeKind.Utc);

        var p1 = new StudentCourseProfile(1, 10, 1);
        var p2 = new StudentCourseProfile(2, 10, 1);
        var p3 = new StudentCourseProfile(3, 10, 1);
        var p4 = new StudentCourseProfile(4, 10, 1);
        var p5 = new StudentCourseProfile(5, 10, 1);
        var p6 = new StudentCourseProfile(6, 10, 1); // 6th place, should get zero badges

        var standings = new List<WeeklySprintStanding>
        {
            new(p1, 1, 100, 10),
            new(p2, 2, 80, 8),
            new(p3, 3, 60, 6),
            new(p4, 4, 40, 4),
            new(p5, 5, 20, 2),
            new(p6, 6, 10, 1)
        };

        var (badgesAwarded, modifiedProfiles) = WeeklySprintCompetition.AwardBadges(standings, cutoff);

        Assert.That(badgesAwarded, Is.EqualTo(15)); // 5 + 4 + 3 + 2 + 1 = 15 badges
        Assert.That(modifiedProfiles.Count, Is.EqualTo(5));

        // p1: Gold, Silver, Bronze, Top4, Top5
        Assert.That(p1.Badges.Select(b => b.BadgeType), Is.EquivalentTo(new[]
        {
            BadgeType.SprintGold, BadgeType.SprintSilver, BadgeType.SprintBronze, BadgeType.SprintTop4, BadgeType.SprintTop5
        }));

        // p2: Silver, Bronze, Top4, Top5
        Assert.That(p2.Badges.Select(b => b.BadgeType), Is.EquivalentTo(new[]
        {
            BadgeType.SprintSilver, BadgeType.SprintBronze, BadgeType.SprintTop4, BadgeType.SprintTop5
        }));

        // p3: Bronze, Top4, Top5
        Assert.That(p3.Badges.Select(b => b.BadgeType), Is.EquivalentTo(new[]
        {
            BadgeType.SprintBronze, BadgeType.SprintTop4, BadgeType.SprintTop5
        }));

        // p4: Top4, Top5
        Assert.That(p4.Badges.Select(b => b.BadgeType), Is.EquivalentTo(new[]
        {
            BadgeType.SprintTop4, BadgeType.SprintTop5
        }));

        // p5: Top5
        Assert.That(p5.Badges.Select(b => b.BadgeType), Is.EquivalentTo(new[]
        {
            BadgeType.SprintTop5
        }));

        // p6: No badges awarded
        Assert.That(p6.Badges, Is.Empty);
    }

    [Test]
    public void AwardBadges_TiedWinners_ShouldBothReceiveSprintGold()
    {
        var cutoff = new DateTime(2026, 10, 23, 23, 59, 59, DateTimeKind.Utc);

        var p1 = new StudentCourseProfile(1, 10, 1);
        var p2 = new StudentCourseProfile(2, 10, 1);

        // Tied score (100) and tied green tests (10)
        var standings = new List<WeeklySprintStanding>
        {
            new(p1, 1, 100, 10),
            new(p2, 2, 100, 10)
        };

        var (badgesAwarded, modifiedProfiles) = WeeklySprintCompetition.AwardBadges(standings, cutoff);

        Assert.That(badgesAwarded, Is.GreaterThan(0));
        Assert.That(modifiedProfiles.Count, Is.EqualTo(2));
        Assert.That(p1.Badges.Any(b => b.BadgeType == BadgeType.SprintGold), Is.True);
        Assert.That(p2.Badges.Any(b => b.BadgeType == BadgeType.SprintGold), Is.True);
    }

    [Test]
    public void AwardBadges_EmptyStandings_ShouldReturnZero()
    {
        var (count, modified) = WeeklySprintCompetition.AwardBadges(
            Array.Empty<WeeklySprintStanding>(), DateTime.UtcNow);

        Assert.That(count, Is.Zero);
        Assert.That(modified, Is.Empty);
    }

    [Test]
    public void DetermineStandings_NullProfiles_ShouldThrowContractException()
    {
        Assert.Throws<ContractException>(() => WeeklySprintCompetition.DetermineStandings(null, DateTime.MinValue, DateTime.MaxValue));
    }

    [Test]
    public void AwardBadges_NullStandings_ShouldThrowContractException()
    {
        Assert.Throws<ContractException>(() => WeeklySprintCompetition.AwardBadges(null, DateTime.UtcNow));
    }
}

