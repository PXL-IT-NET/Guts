using Guts.Common;
using Guts.Domain.GamificationAggregate.Profiles;

namespace Guts.Domain.GamificationAggregate.Competitions;

public class WeeklySprintStanding
{
    public StudentCourseProfile Profile { get; }
    public int Rank { get; }
    public int Score { get; }
    public int GreenTestsCount { get; }

    public WeeklySprintStanding(StudentCourseProfile profile, int rank, int score, int greenTestsCount)
    {
        Contracts.Require(profile != null, "Profile cannot be null.");
        Contracts.Require(rank > 0, "Rank must be positive.");
        Contracts.Require(score > 0, "Score must be positive.");
        Contracts.Require(greenTestsCount >= 0, "Green tests count cannot be negative.");

        Profile = profile;
        Rank = rank;
        Score = score;
        GreenTestsCount = greenTestsCount;
    }
}

