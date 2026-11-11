using System;

namespace Guts.Domain.TopicAggregate.ProjectAggregate;

public static class PeerAssessmentReminderPolicy
{
    public static bool IsAssessmentEligibleForReminders(IProjectAssessment assessment, DateTime nowUtc)
    {
        if (assessment == null)
        {
            return false;
        }

        bool isOpen = assessment.OpenOnUtc <= nowUtc && nowUtc < assessment.DeadlineUtc;
        bool hasMinimumDuration = (assessment.DeadlineUtc - assessment.OpenOnUtc) >= TimeSpan.FromHours(48);
        bool isWithinFinal24Hours = (assessment.DeadlineUtc - nowUtc) <= TimeSpan.FromHours(24);

        return isOpen && hasMinimumDuration && isWithinFinal24Hours;
    }
}

