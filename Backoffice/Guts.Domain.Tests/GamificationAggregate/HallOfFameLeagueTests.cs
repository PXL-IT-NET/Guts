using Guts.Common;
using Guts.Domain.GamificationAggregate;
using NUnit.Framework;

namespace Guts.Domain.Tests.GamificationAggregate;

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
}
