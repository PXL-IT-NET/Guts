using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Guts.Common;
using Guts.Domain.GamificationAggregate.Competitions;
using Guts.Domain.GamificationAggregate.Configuration;
using Guts.Domain.UserAggregate;

namespace Guts.Domain.GamificationAggregate.Profiles;

public class StudentCourseProfile : AggregateRoot
{
    private readonly HashSet<StudentBadge> _badges = [];
    private readonly HashSet<GamificationEvent> _events = [];

    public int UserId { get; private set; }
    public User User { get; set; }

    public int CourseId { get; private set; }
    public int PeriodId { get; private set; }

    public bool ShowInLeaderBoard { get; private set; }
    public bool UsePseudonym { get; private set; }
    public string Pseudonym { get; private set; }
    public string Avatar { get; private set; }

    public int? ClassGroupId { get; private set; }
    public CourseClassGroup ClassGroup { get; set; }

    public int TotalXp { get; private set; }
    public int TotalGreenTests { get; private set; }
    public WeeklyStreak WeeklyStreak { get; private set; } = new WeeklyStreak();
    public DateTime? LastLeaderboardVisitDate { get; private set; }
    public DateTime? LastXpEarnedAt { get; private set; }
    public int? BestHallOfFameRank { get; private set; }
    public HallOfFameTier? BestHallOfFameTier { get; private set; }

    public virtual IReadOnlyCollection<StudentBadge> Badges => _badges;
    public virtual IReadOnlyCollection<GamificationEvent> Events => _events;

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
        _events.Add(new GamificationEvent(Id, GamificationEventType.PassedTest, testId, xp, timestamp));
        if (isStreakEnabled)
        {
            RecordWeeklyActivity(timestamp, isBadgesEnabled);
        }

        if (isBadgesEnabled)
        {
            EvaluatePassedTestBadges(timestamp);
        }

        return true;
    }

    private void EvaluatePassedTestBadges(DateTime timestamp)
    {
        AwardBadge(Badge.FirstSpark, timestamp);

        int totalPassedTests = Events.Count(e => e.EventType == GamificationEventType.PassedTest);
        if (totalPassedTests >= 25)
        {
            AwardBadge(Badge.QuarterCentury, timestamp);
        }

        if (totalPassedTests >= 50)
        {
            AwardBadge(Badge.HalfCentury, timestamp);
        }

        if (totalPassedTests >= 100)
        {
            AwardBadge(Badge.CenturyClub, timestamp);
        }

        if (timestamp.Hour is >= 0 and < 5)
        {
            AwardBadge(Badge.NightOwl, timestamp);
        }
        else if (timestamp.Hour is >= 5 and < 8)
        {
            AwardBadge(Badge.EarlyBird, timestamp);
        }

        if (timestamp.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            AwardBadge(Badge.WeekendWarrior, timestamp);
        }
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
        _events.Add(new GamificationEvent(Id, GamificationEventType.AssignmentCompleted, assignmentId, xp, timestamp));

        if (isBadgesEnabled)
        {
            AwardBadge(Badge.FirstVictory, timestamp);

            int completedAssignmentsCount = Events.Count(e => e.EventType == GamificationEventType.AssignmentCompleted);
            if (completedAssignmentsCount >= 5)
            {
                AwardBadge(Badge.HighFive, timestamp);
            }
        }

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
                _events.Add(new GamificationEvent(Id, GamificationEventType.AssignmentCompletedQuickly, assignmentId, quickBonusXp, timestamp));
                if (isBadgesEnabled)
                {
                    AwardBadge(Badge.SpeedDemon, timestamp);
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
        _events.Add(new GamificationEvent(Id, GamificationEventType.ChapterCompleted, chapterId, xp, timestamp));
        if (isBadgesEnabled)
        {
            AwardBadge(Badge.CleanSweep, timestamp);
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
        _events.Add(new GamificationEvent(Id, GamificationEventType.TeamSynergy, componentId, xp, timestamp));
        if (isBadgesEnabled)
        {
            AwardBadge(Badge.TeamPowerhouse, timestamp);
        }
        return true;
    }

    public bool AwardBadge(Badge badge, DateTime timestamp)
    {
        if (_badges.Any(b => b.Badge == badge))
        {
            return false;
        }

        _badges.Add(new StudentBadge(Id, badge, timestamp));
        return true;
    }

    public HashSet<int> GetPassedTestIds()
    {
        return _events
            .Where(e => e.EventType == GamificationEventType.PassedTest)
            .Select(e => e.TargetId)
            .ToHashSet();
    }

    public bool HasPassedAllTests(IEnumerable<int> testIds)
    {
        Contracts.Require(testIds != null, "TestIds cannot be null.");
        var testIdList = testIds as IReadOnlyCollection<int> ?? testIds.ToList();
        if (testIdList.Count == 0)
        {
            return false;
        }

        var passed = GetPassedTestIds();
        return testIdList.All(passed.Contains);
    }

    public bool HasCompletedAssignment(int assignmentId)
    {
        return _events.Any(e => e.EventType == GamificationEventType.AssignmentCompleted && e.TargetId == assignmentId);
    }

    public bool HasCompletedChapter(int chapterId)
    {
        return _events.Any(e => e.EventType == GamificationEventType.ChapterCompleted && e.TargetId == chapterId);
    }

    internal void AddEvent(GamificationEvent @event)
    {
        Contracts.Require(@event != null, "Event cannot be null.");
        _events.Add(@event);
    }

    public void RecordWeeklyActivity(DateTime timestamp, bool isBadgesEnabled = true)
    {
        WeeklyStreak = WeeklyStreak.RecordActivity(timestamp);

        if (!isBadgesEnabled)
        {
            return;
        }

        if (WeeklyStreak.CurrentStreak >= 2)
        {
            AwardBadge(Badge.WarmingUp, timestamp);
        }

        if (WeeklyStreak.CurrentStreak >= 4)
        {
            AwardBadge(Badge.OnFire, timestamp);
        }

        if (WeeklyStreak.CurrentStreak >= 6)
        {
            AwardBadge(Badge.Unstoppable, timestamp);
        }

        if (WeeklyStreak.CurrentStreak >= 8)
        {
            AwardBadge(Badge.IronWill, timestamp);
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
            hasPassedAllAvailableTests = HasPassedAllTests(allAvailableCourseTestIds);
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
            AwardOvertakeEvents(effectiveBestRank, newRank, timestamp);
        }
        else if (!BestHallOfFameRank.HasValue || previousRank < BestHallOfFameRank.Value)
        {
            BestHallOfFameRank = effectiveBestRank;
        }
    }

    private void AwardOvertakeEvents(int effectiveBestRank, int newRank, DateTime timestamp)
    {
        int overtakesToday = Events.Count(e =>
            e.EventType == GamificationEventType.HallOfFameOvertake &&
            e.CreatedAt.Date == timestamp.Date);

        for (int r = effectiveBestRank - 1; r >= newRank; r--)
        {
            if (HasOvertakeEventForRank(r))
            {
                continue;
            }

            int overtakeXp = GetNextDailyOvertakeXp(overtakesToday);
            overtakesToday++;

            if (overtakeXp > 0)
            {
                TotalXp += overtakeXp;
                LastXpEarnedAt = timestamp;
            }

            _events.Add(new GamificationEvent(Id, GamificationEventType.HallOfFameOvertake, r, overtakeXp, timestamp));
        }
    }

    private bool HasOvertakeEventForRank(int rank)
    {
        return Events.Any(e => e.EventType == GamificationEventType.HallOfFameOvertake && e.TargetId == rank);
    }

    private static int GetNextDailyOvertakeXp(int overtakesToday)
    {
        return overtakesToday switch
        {
            0 => 4,
            1 => 2,
            _ => 0
        };
    }

    public bool EvaluateWeeklyRankDefense(
        int year,
        int weekNumber,
        int currentRank,
        int totalRankedStudents,
        IReadOnlyCollection<int> allAvailableCourseTestIds,
        bool isBadgesEnabled = true)
    {
        Contracts.Require(currentRank > 0, "Current rank must be positive.");
        Contracts.Require(totalRankedStudents >= currentRank, "Total ranked students must be at least currentRank.");

        DateTime startOfWeek = DateTime.SpecifyKind(ISOWeek.ToDateTime(year, weekNumber, DayOfWeek.Monday), DateTimeKind.Utc);
        DateTime endOfWeek = startOfWeek.AddDays(7).AddSeconds(-1);

        if (!IsEligibleForRankDefense(currentRank, totalRankedStudents, startOfWeek, endOfWeek, allAvailableCourseTestIds))
        {
            return false;
        }

        int weekTargetId = (year * 100) + weekNumber;
        if (Events.Any(e => e.EventType == GamificationEventType.HallOfFameRankDefended && e.TargetId == weekTargetId))
        {
            return false;
        }

        const int defenseXp = 5;
        TotalXp += defenseXp;
        LastXpEarnedAt = endOfWeek;
        BestHallOfFameRank = currentRank;
        _events.Add(new GamificationEvent(Id, GamificationEventType.HallOfFameRankDefended, weekTargetId, defenseXp, endOfWeek));

        if (isBadgesEnabled && Events.Count(e => e.EventType == GamificationEventType.HallOfFameRankDefended) >= 3)
        {
            AwardBadge(Badge.RankDefender, endOfWeek);
        }

        return true;
    }

    private bool IsEligibleForRankDefense(
        int currentRank,
        int totalRankedStudents,
        DateTime startOfWeek,
        DateTime endOfWeek,
        IReadOnlyCollection<int> allAvailableCourseTestIds)
    {
        if (!ShowInLeaderBoard || TotalXp <= 0 || !BestHallOfFameRank.HasValue || currentRank > BestHallOfFameRank.Value)
        {
            return false;
        }

        if (currentRank > 1 && currentRank >= totalRankedStudents)
        {
            return false;
        }

        bool wasActiveInWeek = Events.Any(e => e.XpEarned > 0 && e.CreatedAt >= startOfWeek && e.CreatedAt <= endOfWeek);
        if (wasActiveInWeek)
        {
            return true;
        }

        if (allAvailableCourseTestIds is not { Count: > 0 })
        {
            return false;
        }

        return HasPassedAllTests(allAvailableCourseTestIds);
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
                    _events.Add(new GamificationEvent(Id, GamificationEventType.HallOfFameTierUp, t, tierUpXp, timestamp));
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
