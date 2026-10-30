using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Common;
using Guts.Domain.UserAggregate;

namespace Guts.Domain.GamificationAggregate;

public class StudentCourseProfile : AggregateRoot
{
    public int UserId { get; set; }
    public User User { get; set; }

    public int CourseId { get; set; }
    public int PeriodId { get; set; }

    public bool ShowInLeaderBoard { get; private set; }
    public bool UsePseudonym { get; private set; }
    public string Pseudonym { get; private set; }
    public string Avatar { get; private set; }

    public int? ClassGroupId { get; private set; }
    public CourseClassGroup ClassGroup { get; set; }

    public int TotalXp { get; private set; }
    public int TotalGreenTests { get; private set; }
    public WeeklyStreak WeeklyStreak { get; private set; } = new WeeklyStreak();
    public DateTime? LastLeaderboardVisitDate { get; set; }
    public DateTime? LastXpEarnedAt { get; private set; }
    public int? BestHallOfFameRank { get; private set; }
    public HallOfFameTier? BestHallOfFameTier { get; private set; }

    public virtual ICollection<StudentBadge> Badges { get; set; } = new HashSet<StudentBadge>();
    public virtual ICollection<GamificationEvent> Events { get; set; } = new HashSet<GamificationEvent>();

    public int Level => (int)Math.Floor(Math.Sqrt(TotalXp / 25.0)) + 1;

    public string LevelTitle
    {
        get
        {
            return Level switch
            {
                1 => "Byte Novice",
                2 => "Syntax Scout",
                3 => "Logic Apprentice",
                4 => "Code Crafter",
                5 => "Algorithm Ace",
                6 => "Refactor Ranger",
                7 => "System Architect",
                8 => "Bug Whisperer",
                9 => "Full-Stack Guru",
                _ => "Code Titan"
            };
        }
    }

    public StudentCourseProfile() { }

    public StudentCourseProfile(int userId, int courseId, int periodId)
    {
        Contracts.Require(userId > 0, "UserId must be positive.");
        Contracts.Require(courseId > 0, "CourseId must be positive.");
        Contracts.Require(periodId > 0, "PeriodId must be positive.");

        UserId = userId;
        CourseId = courseId;
        PeriodId = periodId;
        ShowInLeaderBoard = true;
        UsePseudonym = false;
        Pseudonym = $"Student_{userId}";
        Avatar = "CodeCat";
        TotalXp = 0;
        WeeklyStreak = new WeeklyStreak();
    }

    public void Enroll(bool showInLeaderBoard, string pseudonym, string avatar, int? classGroupId, bool usePseudonym)
    {
        ShowInLeaderBoard = showInLeaderBoard;
        UsePseudonym = usePseudonym;
        Pseudonym = string.IsNullOrWhiteSpace(pseudonym) ? $"Student_{UserId}" : pseudonym.Trim();
        Avatar = string.IsNullOrWhiteSpace(avatar) ? "CodeCat" : avatar.Trim();
        ClassGroupId = classGroupId;
    }

    public void HideFromLeaderboard()
    {
        ShowInLeaderBoard = false;
    }

    public void ShowOnLeaderboard()
    {
        ShowInLeaderBoard = true;
    }

    public bool RecordLeaderboardVisit(DateTime visitDateTime)
    {
        if (LastLeaderboardVisitDate.HasValue && (visitDateTime - LastLeaderboardVisitDate.Value) <= TimeSpan.FromMinutes(10))
        {
            return false;
        }

        LastLeaderboardVisitDate = visitDateTime;
        return true;
    }

    public bool AwardPassedTestXp(int testId, DateTime timestamp, bool isStreakEnabled = true, bool isBadgesEnabled = true)
    {
        bool alreadyPassed = Events.Any(e => e.EventType == GamificationEventType.PassedTest && e.TargetId == testId);
        if (alreadyPassed)
        {
            return false;
        }

        const int xp = 10;
        TotalXp += xp;
        TotalGreenTests++;
        LastXpEarnedAt = timestamp;
        Events.Add(new GamificationEvent(Id, GamificationEventType.PassedTest, testId, xp, timestamp));
        if (isStreakEnabled)
        {
            RecordWeeklyActivity(timestamp);
        }

        if (isBadgesEnabled)
        {
            AwardBadge(BadgeType.FirstSpark, timestamp);

            int totalPassedTests = Events.Count(e => e.EventType == GamificationEventType.PassedTest);
            if (totalPassedTests >= 100)
            {
                AwardBadge(BadgeType.CenturyClub, timestamp);
            }

            if (timestamp.Hour >= 0 && timestamp.Hour < 5)
            {
                AwardBadge(BadgeType.NightOwl, timestamp);
            }
            else if (timestamp.Hour >= 5 && timestamp.Hour < 8)
            {
                AwardBadge(BadgeType.EarlyBird, timestamp);
            }

            if (timestamp.DayOfWeek == DayOfWeek.Saturday || timestamp.DayOfWeek == DayOfWeek.Sunday)
            {
                AwardBadge(BadgeType.WeekendWarrior, timestamp);
            }
        }

        return true;
    }

    public bool AwardAssignmentBonus(int assignmentId, DateTime timestamp, int? previousCompletionsCount = null, bool isBadgesEnabled = true)
    {
        bool alreadyAwarded = Events.Any(e => e.EventType == GamificationEventType.AssignmentCompleted && e.TargetId == assignmentId);
        if (alreadyAwarded)
        {
            return false;
        }

        const int xp = 50;
        TotalXp += xp;
        LastXpEarnedAt = timestamp;
        Events.Add(new GamificationEvent(Id, GamificationEventType.AssignmentCompleted, assignmentId, xp, timestamp));

        if (previousCompletionsCount.HasValue && previousCompletionsCount.Value >= 0)
        {
            int quickBonusXp = previousCompletionsCount.Value switch
            {
                0 => 50, // 1st student to complete
                1 => 40, // 2nd student to complete
                2 => 30, // 3rd student to complete
                >= 3 and <= 9 => 20, // 4th to 10th student to complete
                _ => 0
            };

            if (quickBonusXp > 0)
            {
                TotalXp += quickBonusXp;
                LastXpEarnedAt = timestamp;
                Events.Add(new GamificationEvent(Id, GamificationEventType.AssignmentCompletedQuickly, assignmentId, quickBonusXp, timestamp));
                if (isBadgesEnabled)
                {
                    AwardBadge(BadgeType.SpeedDemon, timestamp);
                }
            }
        }

        return true;
    }

    public bool AwardChapterBonus(int chapterId, DateTime timestamp, bool isBadgesEnabled = true)
    {
        bool alreadyAwarded = Events.Any(e => e.EventType == GamificationEventType.ChapterCompleted && e.TargetId == chapterId);
        if (alreadyAwarded)
        {
            return false;
        }

        const int xp = 100;
        TotalXp += xp;
        LastXpEarnedAt = timestamp;
        Events.Add(new GamificationEvent(Id, GamificationEventType.ChapterCompleted, chapterId, xp, timestamp));
        if (isBadgesEnabled)
        {
            AwardBadge(BadgeType.CleanSweep, timestamp);
        }
        return true;
    }

    public bool AwardTeamSynergyBonus(int componentId, DateTime timestamp, bool isBadgesEnabled = true)
    {
        bool alreadyAwarded = Events.Any(e => e.EventType == GamificationEventType.TeamSynergy && e.TargetId == componentId);
        if (alreadyAwarded)
        {
            return false;
        }

        const int xp = 30;
        TotalXp += xp;
        LastXpEarnedAt = timestamp;
        Events.Add(new GamificationEvent(Id, GamificationEventType.TeamSynergy, componentId, xp, timestamp));
        if (isBadgesEnabled)
        {
            AwardBadge(BadgeType.TeamPowerhouse, timestamp);
        }
        return true;
    }

    public bool AwardBadge(BadgeType badgeType, DateTime timestamp)
    {
        if (Badges.Any(b => b.BadgeType == badgeType))
        {
            return false;
        }

        Badges.Add(new StudentBadge(Id, badgeType, timestamp));
        return true;
    }

    public void RecordWeeklyActivity(DateTime timestamp)
    {
        WeeklyStreak = WeeklyStreak.RecordActivity(timestamp);

        if (WeeklyStreak.CurrentStreak >= 4)
        {
            AwardBadge(BadgeType.OnFire, timestamp);
        }

        if (WeeklyStreak.CurrentStreak >= 8)
        {
            AwardBadge(BadgeType.IronWill, timestamp);
        }
    }

    public bool NeedsWeeklyStreakEvaluation(DateTime timestamp)
    {
        return WeeklyStreak.NeedsEvaluation(timestamp);
    }

    public bool EvaluateWeeklyStreak(DateTime timestamp, IReadOnlyCollection<int> allAvailableCourseTestIds)
    {
        if (!WeeklyStreak.NeedsEvaluation(timestamp))
        {
            return false;
        }

        bool hasPassedAllAvailableTests = false;
        if (allAvailableCourseTestIds is { Count: > 0 })
        {
            var passedTestIds = Events
                .Where(e => e.EventType == GamificationEventType.PassedTest)
                .Select(e => e.TargetId)
                .ToHashSet();

            hasPassedAllAvailableTests = allAvailableCourseTestIds.All(passedTestIds.Contains);
        }

        WeeklyStreak = WeeklyStreak.EvaluateStreak(timestamp, hasPassedAllAvailableTests);
        return true;
    }

    public bool EvaluateHallOfFameProgression(int previousRank, int newRank, DateTime timestamp)
    {
        Contracts.Require(previousRank > 0, "Previous rank must be positive.");
        Contracts.Require(newRank > 0, "New rank must be positive.");

        int originalXp = TotalXp;

        ApplyOvertakeProgression(previousRank, newRank, timestamp);
        ApplyTierProgression(previousRank, newRank, timestamp);

        return TotalXp != originalXp;
    }

    private void ApplyOvertakeProgression(int previousRank, int newRank, DateTime timestamp)
    {
        int effectiveBestRank = BestHallOfFameRank.HasValue
            ? Math.Min(BestHallOfFameRank.Value, previousRank)
            : previousRank;

        if (newRank < previousRank && newRank < effectiveBestRank)
        {
            BestHallOfFameRank = newRank;
            const int overtakeXp = 4;
            for (int r = effectiveBestRank - 1; r >= newRank; r--)
            {
                if (Events.All(e => !(e.EventType == GamificationEventType.HallOfFameOvertake && e.TargetId == r)))
                {
                    TotalXp += overtakeXp;
                    LastXpEarnedAt = timestamp;
                    Events.Add(new GamificationEvent(Id, GamificationEventType.HallOfFameOvertake, r, overtakeXp, timestamp));
                }
            }
        }
        else if (!BestHallOfFameRank.HasValue || previousRank < BestHallOfFameRank.Value)
        {
            BestHallOfFameRank = effectiveBestRank;
        }
    }

    private void ApplyTierProgression(int previousRank, int newRank, DateTime timestamp)
    {
        HallOfFameTier previousTier = HallOfFameLeague.GetTierForRank(previousRank);
        HallOfFameTier effectiveBestTier = BestHallOfFameTier.HasValue
            ? (HallOfFameTier)Math.Max((int)BestHallOfFameTier.Value, (int)previousTier)
            : previousTier;

        HallOfFameTier newTier = HallOfFameLeague.GetTierForRank(newRank);
        if ((int)newTier > (int)effectiveBestTier)
        {
            BestHallOfFameTier = newTier;
            const int tierUpXp = 5;
            for (int t = (int)effectiveBestTier + 1; t <= (int)newTier; t++)
            {
                if (Events.All(e => !(e.EventType == GamificationEventType.HallOfFameTierUp && e.TargetId == t)))
                {
                    TotalXp += tierUpXp;
                    LastXpEarnedAt = timestamp;
                    Events.Add(new GamificationEvent(Id, GamificationEventType.HallOfFameTierUp, t, tierUpXp, timestamp));
                }
            }
        }
        else if (!BestHallOfFameTier.HasValue || (int)previousTier > (int)BestHallOfFameTier.Value)
        {
            BestHallOfFameTier = effectiveBestTier;
        }
    }

    public class Factory : IStudentCourseProfileFactory
    {
        public StudentCourseProfile CreateNew(int userId, int courseId, int periodId)
        {
            return new StudentCourseProfile(userId, courseId, periodId);
        }
    }
}
