using System;
using System.Collections.Generic;
using Guts.Domain.GamificationAggregate;

namespace Guts.Business.Dtos.Gamification;

public class StudentBadgeDto
{
    public BadgeType BadgeType { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Icon { get; set; }
    public DateTime EarnedAt { get; set; }
}

public class BadgeMetadataDto
{
    public BadgeType BadgeType { get; set; }
    public int Type => (int)BadgeType;
    public string Name { get; set; }
    public string Description { get; set; }
    public string Icon { get; set; }
}

public class RecentXpEventDto
{
    public string Description { get; set; }
    public int XpEarned { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RecentXpGainDto
{
    public int TotalXpGained { get; set; }
    public DateTime? PreviousVisitDate { get; set; }
    public IReadOnlyList<RecentXpEventDto> Events { get; set; } = new List<RecentXpEventDto>();
}

public class StudentGamificationProfileDto
{
    public int UserId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int CourseId { get; set; }
    public int PeriodId { get; set; }
    public bool ShowInLeaderBoard { get; set; }
    public bool UsePseudonym { get; set; }
    public string Pseudonym { get; set; }
    public string Avatar { get; set; }
    public int? ClassGroupId { get; set; }
    public string ClassGroupName { get; set; }
    public int TotalXp { get; set; }
    public int Level { get; set; }
    public string LevelTitle { get; set; }
    public int CurrentWeeklyStreak { get; set; }
    public int BestWeeklyStreak { get; set; }
    public bool IsActiveThisWeek { get; set; }
    public int TotalGreenTests { get; set; }
    public DateTime? LastLeaderboardVisitDate { get; set; }
    public RecentXpGainDto RecentGains { get; set; }
    public IReadOnlyList<StudentBadgeDto> EarnedBadges { get; set; } = new List<StudentBadgeDto>();
}

public class LeaderboardEntryDto
{
    public int Rank { get; set; }
    public int UserId { get; set; }
    public string DisplayName { get; set; }
    public string Avatar { get; set; }
    public string ClassGroupName { get; set; }
    public int Score { get; set; }
    public int Level { get; set; }
    public int WeeklyStreak { get; set; }
    public int GreenTestsCount { get; set; }
    public bool IsCurrentUser { get; set; }
}

public class ClassGroupLeaderboardEntryDto
{
    public int Rank { get; set; }
    public int ClassGroupId { get; set; }
    public string ClassGroupName { get; set; }
    public int StudentCount { get; set; }
    public int TotalGreenTests { get; set; }
    public double AverageXp { get; set; }
    public int TotalXp { get; set; }
}

public class CourseGamificationConfigDto
{
    public int CourseId { get; set; }
    public bool IsGamificationEnabled { get; set; }
    public bool IsWeeklyStreakEnabled { get; set; }
    public bool IsLeaderboardEnabled { get; set; }
    public bool IsClassCompetitionEnabled { get; set; }
    public bool IsTeamSynergyEnabled { get; set; }
    public bool IsBadgesEnabled { get; set; }
}

public class CourseClassGroupDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int CourseId { get; set; }
    public int PeriodId { get; set; }
}

public class EnrollStudentDto
{
    public bool ShowInLeaderBoard { get; set; } = true;
    public bool UsePseudonym { get; set; }
    public string Pseudonym { get; set; }
    public string Avatar { get; set; }
    public int? ClassGroupId { get; set; }
}