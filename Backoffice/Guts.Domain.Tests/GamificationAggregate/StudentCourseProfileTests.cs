using System;
using System.Linq;
using Guts.Common;
using Guts.Domain.GamificationAggregate;
using NUnit.Framework;

namespace Guts.Domain.Tests.GamificationAggregate;

[TestFixture]
public class StudentCourseProfileTests
{
    private StudentCourseProfile.Factory _factory;

    [SetUp]
    public void SetUp()
    {
        _factory = new StudentCourseProfile.Factory();
    }

    [Test]
    public void Factory_CreateNew_ValidArguments_ShouldInitializeCorrectly()
    {
        var profile = _factory.CreateNew(10, 20, 30);

        Assert.That(profile.UserId, Is.EqualTo(10));
        Assert.That(profile.CourseId, Is.EqualTo(20));
        Assert.That(profile.PeriodId, Is.EqualTo(30));
        Assert.That(profile.ShowInLeaderBoard, Is.True);
        Assert.That(profile.TotalXp, Is.Zero);
        Assert.That(profile.Level, Is.EqualTo(1));
        Assert.That(profile.LevelTitle, Is.EqualTo("Byte Novice"));
        Assert.That(profile.Badges, Is.Empty);
        Assert.That(profile.Events, Is.Empty);
    }

    [Test]
    public void Enroll_ShouldUpdatePreferences()
    {
        var profile = _factory.CreateNew(1, 2, 3);
        profile.Enroll(false, "ShadowCoder", "SyntaxFox", 5, true);

        Assert.That(profile.ShowInLeaderBoard, Is.False);
        Assert.That(profile.Pseudonym, Is.EqualTo("ShadowCoder"));
        Assert.That(profile.Avatar, Is.EqualTo("SyntaxFox"));
        Assert.That(profile.ClassGroupId, Is.EqualTo(5));
        Assert.That(profile.UsePseudonym, Is.True);
    }

    [Test]
    public void HideFromLeaderboard_And_ShowOnLeaderboard_ShouldToggleShowInLeaderBoard()
    {
        var profile = _factory.CreateNew(1, 2, 3);
        Assert.That(profile.ShowInLeaderBoard, Is.True);
        profile.HideFromLeaderboard();
        Assert.That(profile.ShowInLeaderBoard, Is.False);
        profile.ShowOnLeaderboard();
        Assert.That(profile.ShowInLeaderBoard, Is.True);
    }

    [Test]
    public void RecordLeaderboardVisit_ShouldUpdateLastLeaderboardVisitDate()
    {
        var profile = _factory.CreateNew(1, 2, 3);
        Assert.That(profile.LastLeaderboardVisitDate, Is.Null);

        var visitTime = new DateTime(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);
        bool updated = profile.RecordLeaderboardVisit(visitTime);

        Assert.That(updated, Is.True);
        Assert.That(profile.LastLeaderboardVisitDate, Is.EqualTo(visitTime));
    }

    [Test]
    public void RecordLeaderboardVisit_WhenWithin10Minutes_ShouldNotUpdateDateAndReturnFalse()
    {
        var profile = _factory.CreateNew(1, 2, 3);
        var initialVisit = new DateTime(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);
        profile.RecordLeaderboardVisit(initialVisit);

        var secondVisit = initialVisit.AddMinutes(5);
        bool updated = profile.RecordLeaderboardVisit(secondVisit);

        Assert.That(updated, Is.False);
        Assert.That(profile.LastLeaderboardVisitDate, Is.EqualTo(initialVisit));
    }

    [Test]
    public void RecordLeaderboardVisit_WhenMoreThan10MinutesLater_ShouldUpdateDateAndReturnTrue()
    {
        var profile = _factory.CreateNew(1, 2, 3);
        var initialVisit = new DateTime(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);
        profile.RecordLeaderboardVisit(initialVisit);

        var secondVisit = initialVisit.AddMinutes(11);
        bool updated = profile.RecordLeaderboardVisit(secondVisit);

        Assert.That(updated, Is.True);
        Assert.That(profile.LastLeaderboardVisitDate, Is.EqualTo(secondVisit));
    }

    [Test]
    public void AwardPassedTestXp_FirstTime_ShouldAdd10XpAndPassedTestEvent()
    {
        var profile = _factory.CreateNew(1, 2, 3);
        var now = new DateTime(2026, 10, 20, 10, 0, 0, DateTimeKind.Utc);

        bool result = profile.AwardPassedTestXp(101, now);

        Assert.That(result, Is.True);
        Assert.That(profile.TotalXp, Is.EqualTo(10));
        Assert.That(profile.Events.Count, Is.EqualTo(1));
        var evt = profile.Events.First();
        Assert.That(evt.EventType, Is.EqualTo(GamificationEventType.PassedTest));
        Assert.That(evt.TargetId, Is.EqualTo(101));
        Assert.That(evt.XpEarned, Is.EqualTo(10));
        Assert.That(profile.Badges.Any(b => b.BadgeType == BadgeType.FirstSpark), Is.True);
    }

    [Test]
    public void AwardPassedTestXp_AlreadyPassed_ShouldReturnFalseAndNotAddXp()
    {
        var profile = _factory.CreateNew(1, 2, 3);
        var now = new DateTime(2026, 10, 20, 10, 0, 0, DateTimeKind.Utc);

        profile.AwardPassedTestXp(101, now);
        bool secondAttempt = profile.AwardPassedTestXp(101, now.AddMinutes(5));

        Assert.That(secondAttempt, Is.False);
        Assert.That(profile.TotalXp, Is.EqualTo(10));
        Assert.That(profile.Events.Count, Is.EqualTo(1));
    }

    [Test]
    public void AwardPassedTestXp_WhenStreakAndBadgesDisabled_ShouldNotRecordStreakOrBadges()
    {
        var profile = _factory.CreateNew(1, 2, 3);
        var now = new DateTime(2026, 10, 20, 10, 0, 0, DateTimeKind.Utc);

        bool result = profile.AwardPassedTestXp(101, now, isStreakEnabled: false, isBadgesEnabled: false);

        Assert.That(result, Is.True);
        Assert.That(profile.TotalXp, Is.EqualTo(10));
        Assert.That(profile.WeeklyStreak.CurrentStreak, Is.Zero);
        Assert.That(profile.Badges, Is.Empty);
    }

    [Test]
    public void RecordWeeklyActivity_4And8Weeks_ShouldAwardOnFireAndIronWillBadges()
    {
        var profile = _factory.CreateNew(1, 2, 3);
        var startMonday = new DateTime(2026, 9, 7, 12, 0, 0, DateTimeKind.Utc);

        for (int w = 0; w < 8; w++)
        {
            profile.RecordWeeklyActivity(startMonday.AddDays(w * 7));
        }

        Assert.That(profile.WeeklyStreak.CurrentStreak, Is.EqualTo(8));
        Assert.That(profile.Badges.Any(b => b.BadgeType == BadgeType.OnFire), Is.True);
        Assert.That(profile.Badges.Any(b => b.BadgeType == BadgeType.IronWill), Is.True);
    }

    [Test]
    public void AwardAssignmentBonus_FirstTime_ShouldAdd50Xp_AndDuplicateShouldReturnFalse()
    {
        var profile = _factory.CreateNew(1, 2, 3);
        var now = DateTime.UtcNow;

        bool result = profile.AwardAssignmentBonus(55, now);
        bool duplicate = profile.AwardAssignmentBonus(55, now.AddMinutes(1));

        Assert.That(result, Is.True);
        Assert.That(duplicate, Is.False);
        Assert.That(profile.TotalXp, Is.EqualTo(50));
        Assert.That(profile.Events.Any(e => e.EventType == GamificationEventType.AssignmentCompleted && e.TargetId == 55), Is.True);
    }

    [TestCase(0, 50, 100)] // 1st student: 50 base + 50 quick = 100
    [TestCase(1, 40, 90)]  // 2nd student: 50 base + 40 quick = 90
    [TestCase(2, 30, 80)]  // 3rd student: 50 base + 30 quick = 80
    [TestCase(3, 20, 70)]  // 4th student: 50 base + 20 quick = 70
    [TestCase(9, 20, 70)]  // 10th student: 50 base + 20 quick = 70
    [TestCase(10, 0, 50)]  // 11th student: 50 base + 0 quick = 50
    [TestCase(25, 0, 50)]  // 26th student: 50 base + 0 quick = 50
    public void AwardAssignmentBonus_WithCompletionRank_ShouldAwardAdditionalQuickBonus(
        int previousCompletions, int expectedQuickXp, int expectedTotalXp)
    {
        var profile = _factory.CreateNew(1, 2, 3);
        var now = DateTime.UtcNow;

        bool result = profile.AwardAssignmentBonus(55, now, previousCompletions);

        Assert.That(result, Is.True);
        Assert.That(profile.TotalXp, Is.EqualTo(expectedTotalXp));
        Assert.That(profile.Events.Any(e => e.EventType == GamificationEventType.AssignmentCompleted && e.TargetId == 55), Is.True);
        if (expectedQuickXp > 0)
        {
            var quickEvt = profile.Events.FirstOrDefault(e => e.EventType == GamificationEventType.AssignmentCompletedQuickly && e.TargetId == 55);
            Assert.That(quickEvt, Is.Not.Null);
            Assert.That(quickEvt.XpEarned, Is.EqualTo(expectedQuickXp));
            Assert.That(profile.Badges.Any(b => b.BadgeType == BadgeType.SpeedDemon), Is.True);
        }
        else
        {
            Assert.That(profile.Events.Any(e => e.EventType == GamificationEventType.AssignmentCompletedQuickly), Is.False);
        }
    }

    [Test]
    public void AwardChapterBonus_FirstTime_ShouldAdd100XpAndCleanSweepBadge_AndDuplicateShouldReturnFalse()
    {
        var profile = _factory.CreateNew(1, 2, 3);
        var now = DateTime.UtcNow;

        bool result = profile.AwardChapterBonus(77, now);
        bool duplicate = profile.AwardChapterBonus(77, now.AddMinutes(1));

        Assert.That(result, Is.True);
        Assert.That(duplicate, Is.False);
        Assert.That(profile.TotalXp, Is.EqualTo(100));
        Assert.That(profile.Badges.Any(b => b.BadgeType == BadgeType.CleanSweep), Is.True);
    }

    [Test]
    public void AwardTeamSynergyBonus_FirstTime_ShouldAdd30XpAndTeamPowerhouseBadge()
    {
        var profile = _factory.CreateNew(1, 2, 3);
        var now = DateTime.UtcNow;

        bool result = profile.AwardTeamSynergyBonus(99, now);

        Assert.That(result, Is.True);
        Assert.That(profile.TotalXp, Is.EqualTo(30));
        Assert.That(profile.Badges.Any(b => b.BadgeType == BadgeType.TeamPowerhouse), Is.True);
    }

    [Test]
    public void AwardTeamSynergyBonus_BadgesDisabled_ShouldAdd30XpWithoutBadge()
    {
        var profile = _factory.CreateNew(1, 2, 3);
        var now = DateTime.UtcNow;

        bool result = profile.AwardTeamSynergyBonus(99, now, isBadgesEnabled: false);

        Assert.That(result, Is.True);
        Assert.That(profile.TotalXp, Is.EqualTo(30));
        Assert.That(profile.Badges.Any(b => b.BadgeType == BadgeType.TeamPowerhouse), Is.False);
    }

    [Test]
    public void AwardTeamSynergyBonus_SecondTime_ShouldReturnFalseAndNotAddDuplicateXp()
    {
        var profile = _factory.CreateNew(1, 2, 3);
        var now = DateTime.UtcNow;

        profile.AwardTeamSynergyBonus(99, now);
        bool secondResult = profile.AwardTeamSynergyBonus(99, now.AddMinutes(1));

        Assert.That(secondResult, Is.False);
        Assert.That(profile.TotalXp, Is.EqualTo(30));
    }

    [TestCase(0, 1, "Byte Novice")]
    [TestCase(30, 2, "Syntax Scout")]
    [TestCase(100, 3, "Logic Apprentice")]
    [TestCase(230, 4, "Code Crafter")]
    [TestCase(400, 5, "Algorithm Ace")]
    [TestCase(630, 6, "Refactor Ranger")]
    [TestCase(900, 7, "System Architect")]
    [TestCase(1230, 8, "Bug Whisperer")]
    [TestCase(1600, 9, "Full-Stack Guru")]
    [TestCase(2030, 10, "Code Titan")]
    public void Level_And_LevelTitle_ShouldCalculateCorrectly(int totalXp, int expectedLevel, string expectedTitle)
    {
        var profile = _factory.CreateNew(1, 2, 3);

        for (int i = 0; i < totalXp / 10; i++)
        {
            profile.AwardPassedTestXp(i + 1, DateTime.UtcNow);
        }

        Assert.That(profile.Level, Is.EqualTo(expectedLevel));
        Assert.That(profile.LevelTitle, Is.EqualTo(expectedTitle));
    }

    [Test]
    public void AwardPassedTestXp_TimeBadges_ShouldTriggerCorrectly()
    {
        var profile = _factory.CreateNew(1, 2, 3);

        // Night Owl (02:00)
        profile.AwardPassedTestXp(1, new DateTime(2026, 10, 21, 2, 30, 0, DateTimeKind.Utc));
        Assert.That(profile.Badges.Any(b => b.BadgeType == BadgeType.NightOwl), Is.True);

        // Early Bird (06:00)
        profile.AwardPassedTestXp(2, new DateTime(2026, 10, 21, 6, 30, 0, DateTimeKind.Utc));
        Assert.That(profile.Badges.Any(b => b.BadgeType == BadgeType.EarlyBird), Is.True);

        // Weekend Warrior (Saturday)
        profile.AwardPassedTestXp(3, new DateTime(2026, 10, 24, 14, 0, 0, DateTimeKind.Utc));
        Assert.That(profile.Badges.Any(b => b.BadgeType == BadgeType.WeekendWarrior), Is.True);
    }

    [Test]
    public void AwardBadge_SprintBadges_ShouldAddBadgeOnce()
    {
        var profile = _factory.CreateNew(1, 2, 3);
        var timestamp = DateTime.UtcNow;

        Assert.That(profile.AwardBadge(BadgeType.SprintGold, timestamp), Is.True);
        Assert.That(profile.AwardBadge(BadgeType.SprintSilver, timestamp), Is.True);
        Assert.That(profile.AwardBadge(BadgeType.SprintBronze, timestamp), Is.True);
        Assert.That(profile.AwardBadge(BadgeType.SprintTop4, timestamp), Is.True);
        Assert.That(profile.AwardBadge(BadgeType.SprintTop5, timestamp), Is.True);

        // Cannot earn duplicate badge
        Assert.That(profile.AwardBadge(BadgeType.SprintGold, timestamp), Is.False);
        Assert.That(profile.Badges.Count(b => b.BadgeType == BadgeType.SprintGold), Is.EqualTo(1));
    }

    [Test]
    public void EvaluateWeeklyStreak_WhenStreakDoesNotNeedEvaluation_ShouldShortCircuitAndReturnFalse()
    {
        var profile = _factory.CreateNew(1, 2, 3);
        var week43 = new DateTime(2026, 10, 19, 10, 0, 0, DateTimeKind.Utc);
        var week44 = week43.AddDays(7);

        profile.AwardPassedTestXp(101, week43);

        Assert.That(profile.NeedsWeeklyStreakEvaluation(week44), Is.False);
        bool changed = profile.EvaluateWeeklyStreak(week44, new[] { 101, 102 });

        Assert.That(changed, Is.False);
        Assert.That(profile.WeeklyStreak.CurrentStreak, Is.EqualTo(1));
    }

    [Test]
    public void EvaluateWeeklyStreak_WhenMissedLastWeekAndNotAllTestsPassed_ShouldResetCurrentStreakToZeroAndReturnTrue()
    {
        var profile = _factory.CreateNew(1, 2, 3);
        var week41 = new DateTime(2026, 10, 5, 10, 0, 0, DateTimeKind.Utc);
        var week42 = week41.AddDays(7);
        var week44 = week41.AddDays(21); // Missed Week 43

        profile.AwardPassedTestXp(101, week41);
        profile.AwardPassedTestXp(102, week42);
        Assert.That(profile.WeeklyStreak.CurrentStreak, Is.EqualTo(2));
        Assert.That(profile.NeedsWeeklyStreakEvaluation(week44), Is.True);

        // Test 103 is not yet passed
        bool changed = profile.EvaluateWeeklyStreak(week44, new[] { 101, 102, 103 });

        Assert.That(changed, Is.True);
        Assert.That(profile.WeeklyStreak.CurrentStreak, Is.Zero);
        Assert.That(profile.WeeklyStreak.BestStreak, Is.EqualTo(2));
        Assert.That(profile.NeedsWeeklyStreakEvaluation(week44), Is.False);
    }

    [Test]
    public void EvaluateWeeklyStreak_WhenMissedLastWeekAndNoCourseTestsExist_ShouldResetCurrentStreakToZero()
    {
        var profile = _factory.CreateNew(1, 2, 3);
        var week42 = new DateTime(2026, 10, 12, 10, 0, 0, DateTimeKind.Utc);
        var week44 = week42.AddDays(14); // Missed Week 43

        profile.RecordWeeklyActivity(week42);
        Assert.That(profile.WeeklyStreak.CurrentStreak, Is.EqualTo(1));

        bool changed = profile.EvaluateWeeklyStreak(week44, Array.Empty<int>());

        Assert.That(changed, Is.True);
        Assert.That(profile.WeeklyStreak.CurrentStreak, Is.Zero);
    }

    [Test]
    public void EvaluateWeeklyStreak_WhenMissedLastWeekButAllAvailableCourseTestsPassed_ShouldKeepStreakAndReturnTrue()
    {
        var profile = _factory.CreateNew(1, 2, 3);
        var week41 = new DateTime(2026, 10, 5, 10, 0, 0, DateTimeKind.Utc);
        var week42 = week41.AddDays(7);
        var week44 = week41.AddDays(21); // Missed Week 43

        profile.AwardPassedTestXp(101, week41);
        profile.AwardPassedTestXp(102, week42);
        Assert.That(profile.WeeklyStreak.CurrentStreak, Is.EqualTo(2));

        // Student has already passed both available course tests (101 and 102)
        bool changed = profile.EvaluateWeeklyStreak(week44, new[] { 101, 102 });

        Assert.That(changed, Is.True);
        Assert.That(profile.WeeklyStreak.CurrentStreak, Is.EqualTo(2));
        Assert.That(profile.WeeklyStreak.BestStreak, Is.EqualTo(2));
        Assert.That(profile.NeedsWeeklyStreakEvaluation(week44), Is.False);
    }

    [Test]
    public void LastXpEarnedAt_ShouldReturnLatestPositiveXpTimestampOrNullWhenNoXpEarned()
    {
        var profile = _factory.CreateNew(1, 2, 3);
        Assert.That(profile.LastXpEarnedAt, Is.Null);

        var firstTime = new DateTime(2026, 10, 20, 10, 0, 0, DateTimeKind.Utc);
        var secondTime = firstTime.AddMinutes(30);

        profile.AwardPassedTestXp(1, firstTime);
        profile.AwardPassedTestXp(2, secondTime);

        Assert.That(profile.LastXpEarnedAt, Is.EqualTo(secondTime));
    }

    [Test]
    public void EvaluateHallOfFameProgression_WhenSurpassingStudentsAndRisingTier_ShouldAward4XpPerRankAnd5XpPerTier()
    {
        var profile = _factory.CreateNew(1, 2, 3);
        var timestamp = new DateTime(2026, 10, 25, 14, 0, 0, DateTimeKind.Utc);

        // Moves from Rank 37 (Gold, tier 6) to Rank 34 (Diamond, tier 7):
        // Surpassed 3 rank positions (36, 35, 34) -> 3 * 4 = 12 XP
        // Rose 1 tier (Gold -> Diamond) -> 1 * 5 = 5 XP
        bool changed = profile.EvaluateHallOfFameProgression(37, 34, timestamp);

        Assert.That(changed, Is.True);
        Assert.That(profile.TotalXp, Is.EqualTo(17));
        Assert.That(profile.BestHallOfFameRank, Is.EqualTo(34));
        Assert.That(profile.BestHallOfFameTier, Is.EqualTo(HallOfFameTier.Diamond));
        Assert.That(profile.Events.Count(e => e.EventType == GamificationEventType.HallOfFameOvertake), Is.EqualTo(3));
        Assert.That(profile.Events.Count(e => e.EventType == GamificationEventType.HallOfFameTierUp), Is.EqualTo(1));
    }

    [Test]
    public void EvaluateHallOfFameProgression_WhenTradingPlacesBackAndForth_ShouldNotReAwardAlreadyAchievedRanksOrTiers()
    {
        var profile = _factory.CreateNew(1, 2, 3);
        var timestamp = new DateTime(2026, 10, 25, 14, 0, 0, DateTimeKind.Utc);

        // First climb: Rank 16 (Diamond) -> Rank 15 (Obsidian): +4 XP (rank 15) + 5 XP (Obsidian) = 9 XP
        profile.EvaluateHallOfFameProgression(16, 15, timestamp);
        Assert.That(profile.TotalXp, Is.EqualTo(9));

        // Later drops back to 16 and climbs to 15 again -> should NOT earn duplicate XP for rank 15 or Obsidian
        bool secondClimb = profile.EvaluateHallOfFameProgression(16, 15, timestamp.AddHours(1));
        Assert.That(secondClimb, Is.False);
        Assert.That(profile.TotalXp, Is.EqualTo(9));

        // Climbs from 15 to 14 -> only rank 14 is new (+4 XP)
        bool thirdClimb = profile.EvaluateHallOfFameProgression(15, 14, timestamp.AddHours(2));
        Assert.That(thirdClimb, Is.True);
        Assert.That(profile.TotalXp, Is.EqualTo(13));
        Assert.That(profile.BestHallOfFameRank, Is.EqualTo(14));
    }

    [TestCase(0, 5)]
    [TestCase(5, 0)]
    [TestCase(-1, 2)]
    public void EvaluateHallOfFameProgression_WithNonPositiveRanks_ShouldThrowContractException(int previousRank, int newRank)
    {
        var profile = _factory.CreateNew(1, 2, 3);
        Assert.That(() => profile.EvaluateHallOfFameProgression(previousRank, newRank, DateTime.UtcNow),
            Throws.InstanceOf<ContractException>());
    }
}
