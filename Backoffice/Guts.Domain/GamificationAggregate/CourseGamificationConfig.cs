using Guts.Common;
using Guts.Domain.CourseAggregate;

namespace Guts.Domain.GamificationAggregate
{
    public class CourseGamificationConfig : AggregateRoot
    {
        public int CourseId { get; set; }
        public Course Course { get; set; }

        public bool IsGamificationEnabled { get; set; } = false;
        public bool IsWeeklyStreakEnabled { get; set; } = true;
        public bool IsLeaderboardEnabled { get; set; } = true;
        public bool IsClassCompetitionEnabled { get; set; } = true;
        public bool IsTeamSynergyEnabled { get; set; } = true;
        public bool IsBadgesEnabled { get; set; } = true;

        public CourseGamificationConfig() { }

        public CourseGamificationConfig(int courseId)
        {
            Contracts.Require(courseId > 0, "CourseId must be positive.");
            CourseId = courseId;
            IsGamificationEnabled = false;
            IsWeeklyStreakEnabled = true;
            IsLeaderboardEnabled = true;
            IsClassCompetitionEnabled = true;
            IsTeamSynergyEnabled = true;
            IsBadgesEnabled = true;
        }
    }
}

