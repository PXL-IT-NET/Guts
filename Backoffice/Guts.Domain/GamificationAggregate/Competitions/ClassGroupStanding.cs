using Guts.Common;
using Guts.Domain.GamificationAggregate.Configuration;

namespace Guts.Domain.GamificationAggregate.Competitions;

public class ClassGroupStanding
{
    public int Rank { get; }
    public CourseClassGroup ClassGroup { get; }
    public int StudentCount { get; }
    public int TotalGreenTests { get; }
    public int ClassScore { get; }
    public int TotalXp { get; }
    public int? CurrentUserContribution { get; }
    public bool IsCurrentUserClass => CurrentUserContribution.HasValue;

    public ClassGroupStanding(
        int rank,
        CourseClassGroup classGroup,
        int studentCount,
        int totalGreenTests,
        int classScore,
        int totalXp,
        int? currentUserContribution)
    {
        Contracts.Require(rank > 0, "Rank must be positive.");
        Contracts.Require(classGroup != null, "Class group cannot be null.");
        Contracts.Require(studentCount >= 0, "Student count cannot be negative.");
        Contracts.Require(totalGreenTests >= 0, "Total green tests cannot be negative.");
        Contracts.Require(classScore >= 0, "Class score cannot be negative.");
        Contracts.Require(totalXp >= 0, "Total XP cannot be negative.");

        Rank = rank;
        ClassGroup = classGroup;
        StudentCount = studentCount;
        TotalGreenTests = totalGreenTests;
        ClassScore = classScore;
        TotalXp = totalXp;
        CurrentUserContribution = currentUserContribution;
    }
}

