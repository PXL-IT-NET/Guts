using Guts.Common;
using Guts.Domain.CourseAggregate;

namespace Guts.Domain.GamificationAggregate.Configuration;

public class CourseGamificationConfig : AggregateRoot
{
    public int CourseId { get; internal set; }
    public Course Course { get; internal set; }

    public bool IsGamificationEnabled { get; internal set; } = false;
    public bool IsWeeklyStreakEnabled { get; internal set; } = true;
    public bool IsLeaderboardEnabled { get; internal set; } = true;
    public bool IsClassCompetitionEnabled { get; internal set; } = true;
    public bool IsTeamSynergyEnabled { get; internal set; } = true;
    public bool IsBadgesEnabled { get; internal set; } = true;

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

    public void UpdateSettings(
        bool isGamificationEnabled,
        bool isWeeklyStreakEnabled,
        bool isLeaderboardEnabled,
        bool isClassCompetitionEnabled,
        bool isTeamSynergyEnabled,
        bool isBadgesEnabled)
    {
        IsGamificationEnabled = isGamificationEnabled;
        IsWeeklyStreakEnabled = isWeeklyStreakEnabled;
        IsLeaderboardEnabled = isLeaderboardEnabled;
        IsClassCompetitionEnabled = isClassCompetitionEnabled;
        IsTeamSynergyEnabled = isTeamSynergyEnabled;
        IsBadgesEnabled = isBadgesEnabled;
    }
}