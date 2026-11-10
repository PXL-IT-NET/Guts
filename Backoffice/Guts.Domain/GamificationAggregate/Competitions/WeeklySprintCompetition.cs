using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Common;
using Guts.Domain.GamificationAggregate.Profiles;
using Guts.Domain.ValueObjects;

namespace Guts.Domain.GamificationAggregate.Competitions;

public static class WeeklySprintCompetition
{
    public const int TopPerformerCutoff = 5;

    public static IReadOnlyList<WeeklySprintStanding> DetermineStandings(
        IEnumerable<StudentCourseProfile> profiles,
        IsoWeek sprintWeek)
    {
        Contracts.Require(sprintWeek != null, "SprintWeek cannot be null.");
        return DetermineStandings(profiles, sprintWeek.StartOfWeek, sprintWeek.FridayCutoff);
    }

    public static IReadOnlyList<WeeklySprintStanding> DetermineStandings(
        IEnumerable<StudentCourseProfile> profiles,
        DateTime startOfWeek,
        DateTime cutoff)
    {
        Contracts.Require(profiles != null, "Profiles cannot be null.");

        DateTime startUtc = startOfWeek.Kind == DateTimeKind.Utc ? startOfWeek : startOfWeek.ToUniversalTime();
        DateTime cutoffUtc = cutoff.Kind == DateTimeKind.Utc ? cutoff : cutoff.ToUniversalTime();

        var scored = profiles.Select(profile =>
            {
                var weeklyEvents = profile.Events
                    .Where(e =>
                    {
                        DateTime eventUtc = e.CreatedAt.Kind == DateTimeKind.Utc ? e.CreatedAt : e.CreatedAt.ToUniversalTime();
                        return eventUtc >= startUtc && eventUtc <= cutoffUtc;
                    })
                    .ToList();
                int weekXp = weeklyEvents.Sum(e => e.XpEarned);
                int greenTests = weeklyEvents.Count(e => e.EventType == GamificationEventType.PassedTest);

                return new
                {
                    Profile = profile,
                    Score = weekXp,
                    GreenTests = greenTests
                };
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.GreenTests)
            .ToList();

        var result = new List<WeeklySprintStanding>(scored.Count);
        for (int i = 0; i < scored.Count; i++)
        {
            var item = scored[i];
            result.Add(new WeeklySprintStanding(item.Profile, i + 1, item.Score, item.GreenTests));
        }

        return result;
    }

    public static (int BadgesAwarded, IReadOnlyList<StudentCourseProfile> ModifiedProfiles) AwardBadges(
        IReadOnlyList<WeeklySprintStanding> standings,
        IsoWeek sprintWeek)
    {
        Contracts.Require(sprintWeek != null, "SprintWeek cannot be null.");
        return AwardBadges(standings, sprintWeek.FridayCutoff);
    }

    public static (int BadgesAwarded, IReadOnlyList<StudentCourseProfile> ModifiedProfiles) AwardBadges(
        IReadOnlyList<WeeklySprintStanding> standings,
        DateTime fridayCutoff)
    {
        Contracts.Require(standings != null, "Standings cannot be null.");

        if (standings.Count == 0)
        {
            return (0, Array.Empty<StudentCourseProfile>());
        }

        int totalBadgesAwarded = 0;
        var modifiedProfiles = new List<StudentCourseProfile>();
        var topEntry = standings[0];
        var topPerformers = standings.Take(TopPerformerCutoff).ToList();

        foreach (var entry in topPerformers)
        {
            int newlyAwarded = AwardSprintBadgesForEntry(
                entry.Profile,
                entry.Rank,
                entry.Score,
                entry.GreenTestsCount,
                topEntry.Score,
                topEntry.GreenTestsCount,
                fridayCutoff);

            if (newlyAwarded > 0)
            {
                totalBadgesAwarded += newlyAwarded;
                modifiedProfiles.Add(entry.Profile);
            }
        }

        return (totalBadgesAwarded, modifiedProfiles);
    }

    private static int AwardSprintBadgesForEntry(
        StudentCourseProfile profile,
        int rank,
        int score,
        int greenTestsCount,
        int topScore,
        int topGreenTestsCount,
        DateTime fridayCutoff)
    {
        int count = 0;

        if ((rank == 1 || (score == topScore && greenTestsCount == topGreenTestsCount)) &&
            profile.AwardBadge(Badge.SprintGold, fridayCutoff))
        {
            count++;
        }

        if (rank <= 2 && profile.AwardBadge(Badge.SprintSilver, fridayCutoff))
        {
            count++;
        }

        if (rank <= 3 && profile.AwardBadge(Badge.SprintBronze, fridayCutoff))
        {
            count++;
        }

        if (rank <= 4 && profile.AwardBadge(Badge.SprintTop4, fridayCutoff))
        {
            count++;
        }

        if (rank <= 5 && profile.AwardBadge(Badge.SprintTop5, fridayCutoff))
        {
            count++;
        }

        return count;
    }
}

