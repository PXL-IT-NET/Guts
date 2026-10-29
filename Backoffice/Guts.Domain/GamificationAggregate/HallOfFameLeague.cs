using System;
using System.Collections.Generic;
using Guts.Common;

namespace Guts.Domain.GamificationAggregate;

public static class HallOfFameLeague
{
    private static readonly IReadOnlyList<HallOfFameTier> AllTiersDescendingList =
    [
        HallOfFameTier.Obsidian,
        HallOfFameTier.Diamond,
        HallOfFameTier.Gold,
        HallOfFameTier.Silver,
        HallOfFameTier.Bronze,
        HallOfFameTier.Iron,
        HallOfFameTier.Wood,
        HallOfFameTier.Paper
    ];

    public static HallOfFameTier GetTierForRank(int rank)
    {
        Contracts.Require(rank > 0, "Rank must be positive.");

        return rank switch
        {
            <= 15 => HallOfFameTier.Obsidian,
            <= 35 => HallOfFameTier.Diamond,
            <= 55 => HallOfFameTier.Gold,
            <= 75 => HallOfFameTier.Silver,
            <= 100 => HallOfFameTier.Bronze,
            <= 150 => HallOfFameTier.Iron,
            <= 200 => HallOfFameTier.Wood,
            _ => HallOfFameTier.Paper
        };
    }

    public static (HallOfFameTier Tier, string Name, int MinRank, int? MaxRank, string Icon) GetTierMetadata(HallOfFameTier tier)
    {
        return tier switch
        {
            HallOfFameTier.Obsidian => (HallOfFameTier.Obsidian, "Obsidian", 1, 15, "🌋"),
            HallOfFameTier.Diamond => (HallOfFameTier.Diamond, "Diamond", 16, 35, "💎"),
            HallOfFameTier.Gold => (HallOfFameTier.Gold, "Gold", 36, 55, "👑"),
            HallOfFameTier.Silver => (HallOfFameTier.Silver, "Silver", 56, 75, "⚔️"),
            HallOfFameTier.Bronze => (HallOfFameTier.Bronze, "Bronze", 76, 100, "🛡️"),
            HallOfFameTier.Iron => (HallOfFameTier.Iron, "Iron", 101, 150, "⚒️"),
            HallOfFameTier.Wood => (HallOfFameTier.Wood, "Wood", 151, 200, "🪵"),
            _ => (HallOfFameTier.Paper, "Paper", 201, null, "📜")
        };
    }

    public static IReadOnlyList<HallOfFameTier> GetAllTiersDescending() => AllTiersDescendingList;

    public static IReadOnlyList<HallOfFameTier> GetVisibleTierWindow(HallOfFameTier currentTier)
    {
        int current = (int)currentTier;
        int start = Math.Clamp(current - 1, (int)HallOfFameTier.Paper, (int)HallOfFameTier.Obsidian - 3);

        return
        [
            (HallOfFameTier)start,
            (HallOfFameTier)(start + 1),
            (HallOfFameTier)(start + 2),
            (HallOfFameTier)(start + 3)
        ];
    }
}

