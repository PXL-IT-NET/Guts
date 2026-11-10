using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Common;
using Guts.Domain.GamificationAggregate.Competitions;
using Guts.Domain.GamificationAggregate.Profiles;
using NUnit.Framework;

namespace Guts.Domain.Tests.GamificationAggregate.Competitions;

[TestFixture]
public class HallOfFameLeagueTests
{
    [TestCase(1, HallOfFameTier.Obsidian)]
    [TestCase(15, HallOfFameTier.Obsidian)]
    [TestCase(16, HallOfFameTier.Diamond)]
    [TestCase(35, HallOfFameTier.Diamond)]
    [TestCase(36, HallOfFameTier.Gold)]
    [TestCase(55, HallOfFameTier.Gold)]
    [TestCase(56, HallOfFameTier.Silver)]
    [TestCase(75, HallOfFameTier.Silver)]
    [TestCase(76, HallOfFameTier.Bronze)]
    [TestCase(100, HallOfFameTier.Bronze)]
    [TestCase(101, HallOfFameTier.Iron)]
    [TestCase(150, HallOfFameTier.Iron)]
    [TestCase(151, HallOfFameTier.Wood)]
    [TestCase(200, HallOfFameTier.Wood)]
    [TestCase(201, HallOfFameTier.Paper)]
    [TestCase(500, HallOfFameTier.Paper)]
    public void GetTierForRank_WithValidRank_ShouldReturnExpectedTier(int rank, HallOfFameTier expectedTier)
    {
        var tier = HallOfFameLeague.GetTierForRank(rank);

        Assert.That(tier, Is.EqualTo(expectedTier));
    }

    [TestCase(0)]
    [TestCase(-5)]
    public void GetTierForRank_WithNonPositiveRank_ShouldThrowContractException(int invalidRank)
    {
        Assert.That(() => HallOfFameLeague.GetTierForRank(invalidRank), Throws.InstanceOf<ContractException>());
    }

    [TestCase(HallOfFameTier.Paper)]
    [TestCase(HallOfFameTier.Wood)]
    [TestCase(HallOfFameTier.Iron)]
    public void GetVisibleTierWindow_AtLowestTiersAndIron_ShouldReturnPaperWoodIronBronze(HallOfFameTier currentTier)
    {
        var window = HallOfFameLeague.GetVisibleTierWindow(currentTier);

        Assert.That(window, Is.EqualTo(new[]
        {
            HallOfFameTier.Paper,
            HallOfFameTier.Wood,
            HallOfFameTier.Iron,
            HallOfFameTier.Bronze
        }));
    }

    [Test]
    public void GetVisibleTierWindow_InMiddleTier_ShouldReturnTwoPreviousCurrentAndOnlyNextTier()
    {
        var bronzeWindow = HallOfFameLeague.GetVisibleTierWindow(HallOfFameTier.Bronze);
        Assert.That(bronzeWindow, Is.EqualTo(new[]
        {
            HallOfFameTier.Wood,
            HallOfFameTier.Iron,
            HallOfFameTier.Bronze,
            HallOfFameTier.Silver
        }));

        var goldWindow = HallOfFameLeague.GetVisibleTierWindow(HallOfFameTier.Gold);
        Assert.That(goldWindow, Is.EqualTo(new[]
        {
            HallOfFameTier.Bronze,
            HallOfFameTier.Silver,
            HallOfFameTier.Gold,
            HallOfFameTier.Diamond
        }));
    }

    [TestCase(HallOfFameTier.Diamond)]
    [TestCase(HallOfFameTier.Obsidian)]
    public void GetVisibleTierWindow_AtTopBoundaries_ShouldReturnSilverGoldDiamondObsidian(HallOfFameTier currentTier)
    {
        var window = HallOfFameLeague.GetVisibleTierWindow(currentTier);

        Assert.That(window, Is.EqualTo(new[]
        {
            HallOfFameTier.Silver,
            HallOfFameTier.Gold,
            HallOfFameTier.Diamond,
            HallOfFameTier.Obsidian
        }));
    }

    [Test]
    public void GetTierMetadata_ShouldReturnCoolIconsAndRankBounds()
    {
        var obsidian = HallOfFameLeague.GetTierMetadata(HallOfFameTier.Obsidian);
        Assert.That(obsidian.Name, Is.EqualTo("Obsidian"));
        Assert.That(obsidian.MinRank, Is.EqualTo(1));
        Assert.That(obsidian.MaxRank, Is.EqualTo(15));
        Assert.That(obsidian.Icon, Is.Not.Empty);

        var paper = HallOfFameLeague.GetTierMetadata(HallOfFameTier.Paper);
        Assert.That(paper.Name, Is.EqualTo("Paper"));
        Assert.That(paper.MinRank, Is.EqualTo(201));
        Assert.That(paper.MaxRank, Is.Null);
        Assert.That(paper.Icon, Is.Not.Empty);
    }

    [Test]
    public void DetermineStandings_ShouldComputeGlobalRank_AndAssignCorrectTiersAndTierRanks()
    {
        var now = DateTime.UtcNow;
        var profiles = new List<StudentCourseProfile>();

        // Create 20 profiles: 1..15 will be Obsidian, 16..20 will be Diamond
        for (int i = 1; i <= 20; i++)
        {
            var p = new StudentCourseProfile(i, 10, 1);
            SetTotalXp(p, 1000 - (i * 10), now.AddMinutes(i));
            profiles.Add(p);
        }

        var standings = HallOfFameLeague.DetermineStandings(profiles);

        Assert.That(standings.Count, Is.EqualTo(20));

        // Global rank 1
        Assert.That(standings[0].GlobalRank, Is.EqualTo(1));
        Assert.That(standings[0].Tier, Is.EqualTo(HallOfFameTier.Obsidian));
        Assert.That(standings[0].TierRank, Is.EqualTo(1));

        // Global rank 15 (last in Obsidian)
        Assert.That(standings[14].GlobalRank, Is.EqualTo(15));
        Assert.That(standings[14].Tier, Is.EqualTo(HallOfFameTier.Obsidian));
        Assert.That(standings[14].TierRank, Is.EqualTo(15));

        // Global rank 16 (first in Diamond)
        Assert.That(standings[15].GlobalRank, Is.EqualTo(16));
        Assert.That(standings[15].Tier, Is.EqualTo(HallOfFameTier.Diamond));
        Assert.That(standings[15].TierRank, Is.EqualTo(1));

        // Global rank 20 (5th in Diamond)
        Assert.That(standings[19].GlobalRank, Is.EqualTo(20));
        Assert.That(standings[19].Tier, Is.EqualTo(HallOfFameTier.Diamond));
        Assert.That(standings[19].TierRank, Is.EqualTo(5));
    }

    [Test]
    public void DetermineRankProgression_WhenNoCompetitors_ShouldReturnRankOneForBoth()
    {
        var target = new StudentCourseProfile(1, 10, 1);
        var (prevRank, newRank) = HallOfFameLeague.DetermineRankProgression(target, 0, null, new[] { target });

        Assert.That(prevRank, Is.EqualTo(1));
        Assert.That(newRank, Is.EqualTo(1));
    }

    [Test]
    public void DetermineRankProgression_WithCompetitors_ShouldDetermineCorrectPreviousAndNewRank()
    {
        var now = DateTime.UtcNow;

        var competitor1 = new StudentCourseProfile(2, 10, 1);
        SetTotalXp(competitor1, 100, now);

        var competitor2 = new StudentCourseProfile(3, 10, 1);
        SetTotalXp(competitor2, 50, now);

        var target = new StudentCourseProfile(1, 10, 1);
        // Target had 40 XP (rank 3), now has 80 XP (rank 2, surpasses competitor2)
        SetTotalXp(target, 80, now);

        var allProfiles = new[] { competitor1, competitor2, target };

        var (prevRank, newRank) = HallOfFameLeague.DetermineRankProgression(target, 40, now.AddHours(-1), allProfiles);

        Assert.That(prevRank, Is.EqualTo(3));
        Assert.That(newRank, Is.EqualTo(2));
    }

    [Test]
    public void CalculateXpToOvertake_TargetBehindCompetitor_ShouldCalculateDifferencePlusOne()
    {
        int needed = HallOfFameLeague.CalculateXpToOvertake(targetScore: 50, competitorAboveScore: 80);
        Assert.That(needed, Is.EqualTo(31));
    }

    [Test]
    public void CalculateXpToOvertake_TargetTiedOrAhead_ShouldReturnAtLeastOne()
    {
        int neededTied = HallOfFameLeague.CalculateXpToOvertake(targetScore: 80, competitorAboveScore: 80);
        Assert.That(neededTied, Is.EqualTo(1));

        int neededAhead = HallOfFameLeague.CalculateXpToOvertake(targetScore: 90, competitorAboveScore: 80);
        Assert.That(neededAhead, Is.EqualTo(1));
    }

    [Test]
    public void EvaluateWeeklyRankDefense_ShouldEvaluateEligibleRankedStudents()
    {
        var now = DateTime.UtcNow;
        var s1 = new StudentCourseProfile(1, 10, 1);
        s1.AwardPassedTestXp(101, now); // +10 XP, active this week
        s1.EvaluateHallOfFameProgression(2, 1, now); // BestHallOfFameRank = 1

        var s2 = new StudentCourseProfile(2, 10, 1);
        s2.AwardPassedTestXp(102, now); // +10 XP
        // Last place out of 2 students -> ineligible for rank defense

        int isoYear = System.Globalization.ISOWeek.GetYear(now);
        int isoWeek = System.Globalization.ISOWeek.GetWeekOfYear(now);

        var modified = HallOfFameLeague.EvaluateWeeklyRankDefense(
            new[] { s1, s2 },
            isoYear,
            isoWeek,
            new[] { 101, 102 },
            isBadgesEnabled: true);

        Assert.That(modified.Count, Is.EqualTo(1));
        Assert.That(modified[0].UserId, Is.EqualTo(1));
        Assert.That(s1.Events.Any(e => e.EventType == GamificationEventType.HallOfFameRankDefended), Is.True);
    }

    [Test]
    public void DetermineStandings_NullProfiles_ShouldThrowContractException()
    {
        Assert.Throws<ContractException>(() => HallOfFameLeague.DetermineStandings(null));
    }

    [Test]
    public void DetermineRankProgression_NullProfile_ShouldThrowContractException()
    {
        Assert.Throws<ContractException>(() => HallOfFameLeague.DetermineRankProgression(null, 10, null, new List<StudentCourseProfile>()));
    }

    private static void SetTotalXp(StudentCourseProfile profile, int targetXp, DateTime timestamp)
    {
        int currentXp = profile.TotalXp;
        int diff = targetXp - currentXp;
        if (diff > 0)
        {
            int testId = 1000 + profile.UserId * 100;
            while (diff >= 10)
            {
                profile.AwardPassedTestXp(testId++, timestamp);
                diff -= 10;
            }
        }
    }
}
