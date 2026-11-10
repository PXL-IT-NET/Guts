using System.Collections.Generic;
using System.Linq;
using Guts.Common;
using Guts.Domain.ValueObjects;

namespace Guts.Domain.GamificationAggregate.Profiles;

public static class StreakAtRiskPolicy
{
    public static bool IsStreakAtRisk(
        StudentCourseProfile profile,
        IsoWeek currentWeek,
        IReadOnlySet<int> allAvailableCourseTestIds)
    {
        Contracts.Require(profile != null, "Profile cannot be null.");
        Contracts.Require(currentWeek != null, "CurrentWeek cannot be null.");
        Contracts.Require(allAvailableCourseTestIds != null, "Available course test IDs cannot be null.");

        if (profile!.WeeklyStreak.CurrentStreak <= 0)
        {
            return false;
        }

        int weeklyXp = profile.Events
            .Where(e => currentWeek.Contains(e.CreatedAt))
            .Sum(e => e.XpEarned);

        if (weeklyXp > 0)
        {
            return false;
        }

        if (allAvailableCourseTestIds.Count > 0 && profile.HasPassedAllTests(allAvailableCourseTestIds))
        {
            return false;
        }

        return true;
    }
}

