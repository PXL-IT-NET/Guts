using System.Collections.Generic;
using Guts.Domain.GamificationAggregate.Profiles;

namespace Guts.Domain.GamificationAggregate.Competitions;

public static class TeamSynergyPolicy
{
    public static bool IsSynergyAchieved(
        IReadOnlySet<int> assignmentTestIds,
        int requiredTeamMemberCount,
        IReadOnlyCollection<StudentCourseProfile> teamProfiles)
    {
        if (assignmentTestIds == null || assignmentTestIds.Count == 0)
        {
            return false;
        }

        if (teamProfiles == null || requiredTeamMemberCount <= 0 || teamProfiles.Count != requiredTeamMemberCount)
        {
            return false;
        }

        var allTeamPassedTests = new HashSet<int>();

        foreach (var profile in teamProfiles)
        {
            bool memberContributed = false;
            foreach (var evt in profile.Events)
            {
                if (evt.EventType == GamificationEventType.PassedTest && assignmentTestIds.Contains(evt.TargetId))
                {
                    memberContributed = true;
                    allTeamPassedTests.Add(evt.TargetId);
                }
            }

            if (!memberContributed)
            {
                return false;
            }
        }

        return assignmentTestIds.IsSubsetOf(allTeamPassedTests);
    }
}

