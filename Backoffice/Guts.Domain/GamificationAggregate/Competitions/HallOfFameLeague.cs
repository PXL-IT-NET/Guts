using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Common;
using Guts.Domain.GamificationAggregate.Profiles;
using Guts.Domain.ValueObjects;

namespace Guts.Domain.GamificationAggregate.Competitions;

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
        int start = Math.Clamp(current - 2, (int)HallOfFameTier.Paper, (int)HallOfFameTier.Obsidian - 3);

        return
        [
            (HallOfFameTier)start,
            (HallOfFameTier)(start + 1),
            (HallOfFameTier)(start + 2),
            (HallOfFameTier)(start + 3)
        ];
    }

    public static IReadOnlyList<HallOfFameStanding> DetermineStandings(IEnumerable<StudentCourseProfile> profiles)
    {
        Contracts.Require(profiles != null, "Profiles cannot be null.");

        var ordered = OrderProfiles(profiles);
        var entries = new List<HallOfFameStanding>(ordered.Count);
        var tierRankCounters = new Dictionary<HallOfFameTier, int>();

        for (int i = 0; i < ordered.Count; i++)
        {
            var profile = ordered[i];
            int globalRank = i + 1;
            var tier = GetTierForRank(globalRank);
            int tierRank = tierRankCounters.GetValueOrDefault(tier, 0) + 1;
            tierRankCounters[tier] = tierRank;

            entries.Add(new HallOfFameStanding(profile, globalRank, tier, tierRank));
        }

        return entries;
    }

    public static (int PreviousRank, int NewRank) DetermineRankProgression(
        StudentCourseProfile profile,
        int xpBefore,
        DateTime? lastXpBefore,
        IEnumerable<StudentCourseProfile> allOptedInProfiles)
    {
        Contracts.Require(profile != null, "Profile cannot be null.");
        Contracts.Require(allOptedInProfiles != null, "All opted-in profiles cannot be null.");

        var competitors = allOptedInProfiles
            .Where(p => p.UserId != profile.UserId)
            .Select(p => (p.UserId, p.Id, Score: p.TotalXp, LastXpAt: p.LastXpEarnedAt ?? DateTime.MaxValue))
            .ToList();

        if (competitors.Count == 0)
        {
            return (1, 1);
        }

        var beforeOrdering = competitors
            .Append((profile.UserId, profile.Id, Score: xpBefore, LastXpAt: lastXpBefore ?? DateTime.MaxValue))
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.LastXpAt)
            .ThenBy(x => x.Id)
            .ThenBy(x => x.UserId)
            .ToList();

        var afterOrdering = competitors
            .Append((profile.UserId, profile.Id, Score: profile.TotalXp, LastXpAt: profile.LastXpEarnedAt ?? DateTime.MaxValue))
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.LastXpAt)
            .ThenBy(x => x.Id)
            .ThenBy(x => x.UserId)
            .ToList();

        int previousRank = beforeOrdering.FindIndex(x => x.UserId == profile.UserId) + 1;
        int newRank = afterOrdering.FindIndex(x => x.UserId == profile.UserId) + 1;

        return (previousRank, newRank);
    }

    public static int CalculateXpToOvertake(int targetScore, int competitorAboveScore)
    {
        return Math.Max(1, (competitorAboveScore - targetScore) + 1);
    }

    public static IReadOnlyList<StudentCourseProfile> EvaluateWeeklyRankDefense(
        IEnumerable<StudentCourseProfile> optedInProfiles,
        int year,
        int weekNumber,
        IReadOnlyList<int> courseTestIds,
        bool isBadgesEnabled)
    {
        return EvaluateWeeklyRankDefense(optedInProfiles, IsoWeek.From(year, weekNumber), courseTestIds, isBadgesEnabled);
    }

    public static IReadOnlyList<StudentCourseProfile> EvaluateWeeklyRankDefense(
        IEnumerable<StudentCourseProfile> optedInProfiles,
        IsoWeek week,
        IReadOnlyList<int> courseTestIds,
        bool isBadgesEnabled)
    {
        Contracts.Require(optedInProfiles != null, "Opted-in profiles cannot be null.");
        Contracts.Require(week != null, "IsoWeek cannot be null.");

        var rankedProfiles = OrderProfiles(optedInProfiles!.Where(p => p.TotalXp > 0));
        if (rankedProfiles.Count == 0)
        {
            return Array.Empty<StudentCourseProfile>();
        }

        var modifiedProfiles = new List<StudentCourseProfile>();
        for (int i = 0; i < rankedProfiles.Count; i++)
        {
            var profile = rankedProfiles[i];
            int currentRank = i + 1;
            if (profile.EvaluateWeeklyRankDefense(
                    week,
                    currentRank,
                    rankedProfiles.Count,
                    courseTestIds,
                    isBadgesEnabled))
            {
                modifiedProfiles.Add(profile);
            }
        }

        return modifiedProfiles;
    }

    private static List<StudentCourseProfile> OrderProfiles(IEnumerable<StudentCourseProfile> profiles)
    {
        return profiles
            .OrderByDescending(p => p.TotalXp)
            .ThenBy(p => p.LastXpEarnedAt ?? DateTime.MaxValue)
            .ThenBy(p => p.Id)
            .ThenBy(p => p.UserId)
            .ToList();
    }
}
