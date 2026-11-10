using Guts.Domain.AssignmentAggregate;
using Guts.Domain.CourseAggregate;
using System.Diagnostics.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Application.Dtos.Gamification;
using Guts.Application.Repositories;
using Guts.Common;
using Guts.Domain.GamificationAggregate;
using Guts.Domain.GamificationAggregate.Competitions;
using Guts.Domain.GamificationAggregate.Configuration;
using Guts.Domain.GamificationAggregate.Profiles;
using Guts.Domain.PeriodAggregate;
using Guts.Domain.TestRunAggregate;
using Guts.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Guts.Application.Services;

[method: SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters", Justification = "Service orchestrates gamification domain dependencies; preserving backward compatibility.")]
public class GamificationService(
    IStudentCourseProfileRepository studentCourseProfileRepository,
    ICourseGamificationConfigRepository courseGamificationConfigRepository,
    ICourseClassGroupRepository courseClassGroupRepository,
    ICourseRepository courseRepository,
    IProjectTeamRepository projectTeamRepository,
    IAssignmentRepository assignmentRepository,
    IPeriodRepository periodRepository,
    IStudentCourseProfileFactory studentCourseProfileFactory,
    ICourseClassGroupFactory courseClassGroupFactory,
    ILogger<GamificationService> logger)
    : IGamificationService
{
    public async Task ProcessTestRunAsync(TestRun testRun)
    {
        if (testRun?.TestResults == null || !testRun.TestResults.Any(tr => tr.Passed))
        {
            return;
        }

        try
        {
            if (await studentCourseProfileRepository.IsUserLectorAsync(testRun.UserId))
            {
                return;
            }

            var assignment = await assignmentRepository.GetSingleWithTestsAndCourseAsync(testRun.AssignmentId);
            if (assignment?.Topic == null)
            {
                return;
            }

            int courseId = assignment.Topic.CourseId;
            int periodId = assignment.Topic.PeriodId;

            var config = await courseGamificationConfigRepository.GetByCourseIdAsync(courseId);
            if (config == null || !config.IsGamificationEnabled)
            {
                return;
            }

            var profile = await GetOrCreateProfileAsync(testRun.UserId, courseId, periodId);
            int xpBefore = profile.TotalXp;
            DateTime? lastXpBefore = profile.LastXpEarnedAt;

            AwardPassedTestsXp(profile, testRun, config);

            var allAssignmentTestIds = assignment.Tests.Select(t => t.Id).ToHashSet();

            bool allTestsOfAssignmentPassed = await CheckAssignmentAndTopicCompletionAsync(
                profile, assignment, courseId, periodId, testRun.CreateDateTime, config, allAssignmentTestIds);

            if (config.IsTeamSynergyEnabled && allTestsOfAssignmentPassed)
            {
                await CheckTeamSynergyBonusAsync(profile, assignment, allAssignmentTestIds, courseId, periodId, testRun, config);
            }

            if (config.IsLeaderboardEnabled && profile.ShowInLeaderBoard && profile.TotalXp > xpBefore)
            {
                await EvaluateHallOfFameRankingAfterTestRunAsync(
                    profile, courseId, periodId, xpBefore, lastXpBefore, testRun.CreateDateTime);
            }

            await studentCourseProfileRepository.UpdateAsync(profile);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process gamification for test run {TestRunId}", testRun.Id);
        }
    }

    private async Task EvaluateHallOfFameRankingAfterTestRunAsync(
        StudentCourseProfile profile,
        int courseId,
        int periodId,
        int xpBefore,
        DateTime? lastXpBefore,
        DateTime timestamp)
    {
        var optedInProfiles = await studentCourseProfileRepository.GetForLeaderboardByCourseAndPeriodAsync(courseId, periodId);
        if (optedInProfiles == null || optedInProfiles.Count == 0)
        {
            return;
        }

        var (previousRank, newRank) = HallOfFameLeague.DetermineRankProgression(profile, xpBefore, lastXpBefore, optedInProfiles);
        if (previousRank > 0 && newRank > 0)
        {
            profile.EvaluateHallOfFameProgression(previousRank, newRank, timestamp);
        }
    }

    private async Task<StudentCourseProfile> GetOrCreateProfileAsync(int userId, int courseId, int periodId)
    {
        var profile = await studentCourseProfileRepository.GetByUserCourseAndPeriodAsync(userId, courseId, periodId);
        if (profile == null)
        {
            profile = studentCourseProfileFactory.CreateNew(userId, courseId, periodId);
            profile = await studentCourseProfileRepository.AddAsync(profile);
        }
        return profile;
    }

    private static void AwardPassedTestsXp(StudentCourseProfile profile, TestRun testRun, CourseGamificationConfig config)
    {
        foreach (var passingResult in testRun.TestResults.Where(tr => tr.Passed))
        {
            profile.AwardPassedTestXp(passingResult.TestId, testRun.CreateDateTime, config.IsWeeklyStreakEnabled, config.IsBadgesEnabled);
        }
    }

    private async Task<bool> CheckAssignmentAndTopicCompletionAsync(
        StudentCourseProfile profile,
        Assignment assignment,
        int courseId,
        int periodId,
        DateTime testRunDateTime,
        CourseGamificationConfig config,
        HashSet<int> allAssignmentTestIds)
    {
        bool allTestsOfAssignmentPassed = allAssignmentTestIds.Count > 0 && profile.HasPassedAllTests(allAssignmentTestIds);
        if (allTestsOfAssignmentPassed)
        {
            int previousCompletions = await studentCourseProfileRepository.CountAssignmentCompletionsAsync(assignment.Id, courseId, periodId);
            profile.AwardAssignmentBonus(assignment.Id, testRunDateTime, previousCompletions, config.IsBadgesEnabled);

            var topicAssignments = await assignmentRepository.GetByTopicWithTests(assignment.TopicId);
            var allTopicTestIds = topicAssignments.SelectMany(a => a.Tests).Select(t => t.Id).ToHashSet();
            if (allTopicTestIds.Count > 0 && profile.HasPassedAllTests(allTopicTestIds))
            {
                profile.AwardChapterBonus(assignment.TopicId, testRunDateTime, config.IsBadgesEnabled);
            }
        }
        return allTestsOfAssignmentPassed;
    }

    private async Task CheckTeamSynergyBonusAsync(
        StudentCourseProfile profile,
        Assignment assignment,
        HashSet<int> allAssignmentTestIds,
        int courseId,
        int periodId,
        TestRun testRun,
        CourseGamificationConfig config)
    {
        var userTeams = await projectTeamRepository.GetByUserAsync(testRun.UserId);
        var team = userTeams?.FirstOrDefault(t => t.ProjectId == assignment.TopicId);

        if (team?.TeamUsers == null || team.TeamUsers.Count == 0) return;

        var teamUserIds = team.TeamUsers.Select(tu => tu.UserId).Distinct().ToList();
        var teamProfiles = new List<StudentCourseProfile>();
        foreach (var memberId in teamUserIds)
        {
            var memberProfile = memberId == testRun.UserId
                ? profile
                : await studentCourseProfileRepository.GetByUserCourseAndPeriodAsync(memberId, courseId, periodId);

            if (memberProfile != null)
            {
                teamProfiles.Add(memberProfile);
            }
        }

        if (!TeamSynergyPolicy.IsSynergyAchieved(allAssignmentTestIds, teamUserIds.Count, teamProfiles))
        {
            return;
        }

        foreach (var memberProfile in teamProfiles.Where(p => p.AwardTeamSynergyBonus(assignment.Id, testRun.CreateDateTime, config.IsBadgesEnabled)))
        {
            if (memberProfile.UserId != testRun.UserId)
            {
                await studentCourseProfileRepository.UpdateAsync(memberProfile);
            }
        }
    }

    public async Task<StudentGamificationProfileDto> GetStudentCourseProfileAsync(
        int userId,
        int courseId,
        int? periodId = null,
        bool isLector = false,
        int? perspectiveUserId = null)
    {
        int targetUserId = isLector && perspectiveUserId.HasValue ? perspectiveUserId.Value : userId;
        var period = await periodRepository.GetPeriodAsync(periodId);
        var profile = await studentCourseProfileRepository.GetByUserCourseAndPeriodAsync(targetUserId, courseId, period.Id);

        if (profile == null)
        {
            profile = studentCourseProfileFactory.CreateNew(targetUserId, courseId, period.Id);
            profile = await studentCourseProfileRepository.AddAsync(profile);
        }

        if (profile.User == null)
        {
            profile = await studentCourseProfileRepository.GetByUserCourseAndPeriodAsync(targetUserId, courseId, period.Id) ?? profile;
        }

        var previousVisit = profile.LastLeaderboardVisitDate;

        var recentEvents = previousVisit.HasValue
            ? profile.Events.Where(e => e.CreatedAt > previousVisit.Value).OrderByDescending(e => e.CreatedAt).ToList()
            : profile.Events.OrderByDescending(e => e.CreatedAt).ToList();

        var recentEventDtos = BuildRecentEventDtos(recentEvents);

        var recentGains = new RecentXpGainDto
        {
            TotalXpGained = recentEvents.Sum(e => e.XpEarned),
            PreviousVisitDate = previousVisit,
            Events = recentEventDtos
        };

        DateTime now = DateTime.UtcNow;
        bool streakUpdated = await EvaluateSingleProfileWeeklyStreakIfNeededAsync(profile, courseId, period.Id, now);
        bool visitUpdated = (!isLector || !perspectiveUserId.HasValue) && profile.RecordLeaderboardVisit(now);

        if (streakUpdated || visitUpdated)
        {
            await studentCourseProfileRepository.UpdateAsync(profile);
        }

        var dto = MapToProfileDto(profile);
        dto.LastLeaderboardVisitDate = previousVisit;
        dto.RecentGains = recentGains;
        return dto;
    }

    private async Task<bool> EvaluateSingleProfileWeeklyStreakIfNeededAsync(
        StudentCourseProfile profile,
        int courseId,
        int periodId,
        DateTime timestamp)
    {
        if (!profile.NeedsWeeklyStreakEvaluation(timestamp))
        {
            return false;
        }

        bool hasPassedAnyTests = profile.Events.Any(e => e.EventType == GamificationEventType.PassedTest);
        IReadOnlyList<int> courseTestIds = hasPassedAnyTests
            ? await assignmentRepository.GetCourseTestIdsAsync(courseId, periodId)
            : Array.Empty<int>();

        return profile.EvaluateWeeklyStreak(timestamp, courseTestIds);
    }

    private static List<RecentXpEventDto> BuildRecentEventDtos(IReadOnlyList<GamificationEvent> recentEvents)
    {
        var recentEventDtos = new List<RecentXpEventDto>();

        var passedTestEvents = recentEvents
            .Where(e => e.EventType == GamificationEventType.PassedTest)
            .ToList();

        if (passedTestEvents.Count > 0)
        {
            int count = passedTestEvents.Count;
            string testWord = count == 1 ? "test" : "tests";
            recentEventDtos.Add(new RecentXpEventDto
            {
                Description = $"Made {count} {testWord} green",
                XpEarned = passedTestEvents.Sum(e => e.XpEarned),
                CreatedAt = passedTestEvents.Max(e => e.CreatedAt)
            });
        }

        var overtakeEvents = recentEvents
            .Where(e => e.EventType == GamificationEventType.HallOfFameOvertake)
            .ToList();

        int overtakeXp = overtakeEvents.Sum(e => e.XpEarned);
        if (overtakeEvents.Count > 0 && overtakeXp > 0)
        {
            int count = overtakeEvents.Count;
            string studentWord = count == 1 ? "student" : "students";
            recentEventDtos.Add(new RecentXpEventDto
            {
                Description = $"Overtake bonus (surpassed {count} {studentWord} in Hall of Fame)",
                XpEarned = overtakeXp,
                CreatedAt = overtakeEvents.Max(e => e.CreatedAt)
            });
        }

        foreach (var evt in recentEvents.Where(e =>
                     e.EventType != GamificationEventType.PassedTest &&
                     e.EventType != GamificationEventType.HallOfFameOvertake))
        {
            string desc = evt.EventType switch
            {
                GamificationEventType.AssignmentCompleted => "Completed assignment bonus",
                GamificationEventType.AssignmentCompletedQuickly => "Quick assignment completion bonus (Top 10)",
                GamificationEventType.ChapterCompleted => "Completed chapter bonus",
                GamificationEventType.TeamSynergy => "Team synergy bonus",
                GamificationEventType.HallOfFameTierUp =>
                    $"Hall of Fame level up bonus ({HallOfFameLeague.GetTierMetadata((HallOfFameTier)evt.TargetId).Name})",
                GamificationEventType.HallOfFameRankDefended => "Hall of Fame rank defense bonus",
                _ => "Gamification bonus"
            };

            recentEventDtos.Add(new RecentXpEventDto
            {
                Description = desc,
                XpEarned = evt.XpEarned,
                CreatedAt = evt.CreatedAt
            });
        }

        return recentEventDtos.OrderByDescending(e => e.CreatedAt).ToList();
    }

    public async Task<StudentGamificationProfileDto> EnrollStudentAsync(int userId, int courseId, EnrollStudentDto enrollDto, int? periodId = null)
    {
        Contracts.Require(enrollDto != null, "Enrollment data is required.");
        ArgumentNullException.ThrowIfNull(enrollDto);

        var period = await periodRepository.GetPeriodAsync(periodId);
        var profile = await studentCourseProfileRepository.GetByUserCourseAndPeriodAsync(userId, courseId, period.Id);

        if (profile == null)
        {
            profile = studentCourseProfileFactory.CreateNew(userId, courseId, period.Id);
            profile.Enroll(enrollDto.ShowInLeaderBoard, enrollDto.Pseudonym, enrollDto.Avatar, enrollDto.ClassGroupId, enrollDto.UsePseudonym);
            profile = await studentCourseProfileRepository.AddAsync(profile);
        }
        else
        {
            profile.Enroll(enrollDto.ShowInLeaderBoard, enrollDto.Pseudonym, enrollDto.Avatar, enrollDto.ClassGroupId, enrollDto.UsePseudonym);
            profile = await studentCourseProfileRepository.UpdateAsync(profile);
        }

        if (profile.ClassGroupId.HasValue && (profile.ClassGroup == null || profile.ClassGroup.Id != profile.ClassGroupId.Value))
        {
            var classGroup = await courseClassGroupRepository.GetByIdAsync(profile.ClassGroupId.Value);
            profile.AssignClassGroup(classGroup);
        }
        else if (!profile.ClassGroupId.HasValue)
        {
            profile.AssignClassGroup(null);
        }

        if (profile.User == null)
        {
            profile = await studentCourseProfileRepository.GetByUserCourseAndPeriodAsync(userId, courseId, period.Id) ?? profile;
        }

        return MapToProfileDto(profile);
    }

    public async Task<IReadOnlyList<LeaderboardEntryDto>> GetWeeklySprintLeaderboardAsync(int courseId, int currentUserId, int? periodId = null)
    {
        var config = await courseGamificationConfigRepository.GetByCourseIdAsync(courseId);
        if (config != null && (!config.IsGamificationEnabled || !config.IsWeeklyStreakEnabled || !config.IsLeaderboardEnabled))
        {
            return new List<LeaderboardEntryDto>();
        }

        IPeriod period = await periodRepository.GetPeriodAsync(periodId);
        DateTime now = DateTime.UtcNow;
        IsoWeek currentWeek = IsoWeek.From(now);
        DateTime startOfWeek = currentWeek.StartOfWeek;
        IReadOnlyList<StudentCourseProfile> optedInProfiles = await studentCourseProfileRepository.GetForWeeklySprintLeaderboardAsync(courseId, period.Id, startOfWeek);

        var sprintEntries = WeeklySprintCompetition.DetermineStandings(optedInProfiles, startOfWeek, now);

        return sprintEntries.Select(item => new LeaderboardEntryDto
        {
            Rank = item.Rank,
            UserId = item.Profile.UserId,
            DisplayName = GetDisplayName(item.Profile),
            Avatar = item.Profile.Avatar,
            ClassGroupName = item.Profile.ClassGroup?.Name ?? "Unassigned",
            Score = item.Score,
            Level = item.Profile.Level,
            WeeklyStreak = item.Profile.WeeklyStreak.CurrentStreak,
            GreenTestsCount = item.GreenTestsCount,
            IsCurrentUser = item.Profile.UserId == currentUserId
        }).ToList();
    }

    public async Task<HallOfFameLeaderboardDto> GetHallOfFameLeaderboardAsync(
        int courseId,
        int currentUserId,
        int? periodId = null,
        bool isLector = false,
        int? perspectiveUserId = null,
        HallOfFameTier? selectedTier = null)
    {
        var config = await courseGamificationConfigRepository.GetByCourseIdAsync(courseId);
        if (config != null && (!config.IsGamificationEnabled || !config.IsLeaderboardEnabled))
        {
            return new HallOfFameLeaderboardDto();
        }

        var period = await periodRepository.GetPeriodAsync(periodId);
        var optedInProfiles = await studentCourseProfileRepository.GetForLeaderboardByCourseAndPeriodAsync(courseId, period.Id);

        var rankedEntries = HallOfFameLeague.DetermineStandings(optedInProfiles);

        int effectiveTargetUserId = isLector ? (perspectiveUserId ?? 0) : currentUserId;
        var allEntries = BuildHallOfFameEntries(rankedEntries, effectiveTargetUserId);
        var tierCounts = allEntries
            .GroupBy(e => e.Tier)
            .ToDictionary(g => g.Key, g => g.Count());

        var allTiers = HallOfFameLeague.GetAllTiersDescending()
            .Select(tier => MapToTierDto(tier, false, tierCounts.GetValueOrDefault(tier, 0)))
            .ToList();

        var studentOptions = isLector
            ? allEntries.Select(e => new HallOfFameStudentOptionDto
            {
                UserId = e.UserId,
                DisplayName = e.DisplayName,
                Rank = e.Rank,
                Score = e.Score,
                TierName = e.TierName,
                TierIcon = e.TierIcon
            }).ToList()
            : new List<HallOfFameStudentOptionDto>();

        bool isStudentPerspective = !isLector || perspectiveUserId.HasValue;
        if (isStudentPerspective)
        {
            return BuildStudentPerspectiveHallOfFame(allEntries, allTiers, tierCounts, studentOptions, effectiveTargetUserId);
        }

        return BuildLectorOverviewHallOfFame(allEntries, allTiers, tierCounts, studentOptions, selectedTier);
    }

    private static List<LeaderboardEntryDto> BuildHallOfFameEntries(
        IReadOnlyList<HallOfFameStanding> rankedEntries,
        int effectiveTargetUserId)
    {
        var allEntries = new List<LeaderboardEntryDto>(rankedEntries.Count);

        foreach (var rankEntry in rankedEntries)
        {
            var profile = rankEntry.Profile;
            var tierMeta = HallOfFameLeague.GetTierMetadata(rankEntry.Tier);

            allEntries.Add(new LeaderboardEntryDto
            {
                Rank = rankEntry.TierRank,
                UserId = profile.UserId,
                DisplayName = GetDisplayName(profile),
                Avatar = profile.Avatar,
                ClassGroupName = profile.ClassGroup?.Name ?? "Unassigned",
                Score = profile.TotalXp,
                Level = profile.Level,
                WeeklyStreak = profile.WeeklyStreak.CurrentStreak,
                GreenTestsCount = profile.TotalGreenTests,
                IsCurrentUser = profile.UserId == effectiveTargetUserId,
                Tier = rankEntry.Tier,
                TierName = tierMeta.Name,
                TierIcon = tierMeta.Icon
            });
        }

        return allEntries;
    }

    private static HallOfFameLeaderboardDto BuildStudentPerspectiveHallOfFame(
        List<LeaderboardEntryDto> allEntries,
        IReadOnlyList<HallOfFameTierDto> allTiers,
        Dictionary<HallOfFameTier, int> tierCounts,
        IReadOnlyList<HallOfFameStudentOptionDto> studentOptions,
        int targetUserId)
    {
        int targetIndex = -1;
        LeaderboardEntryDto targetEntry = null;
        for (int i = 0; i < allEntries.Count; i++)
        {
            if (allEntries[i].UserId == targetUserId)
            {
                targetIndex = i;
                targetEntry = allEntries[i];
                break;
            }
        }

        HallOfFameTier activeTier = targetEntry?.Tier ?? HallOfFameTier.Paper;
        var currentTierDto = MapToTierDto(activeTier, true, tierCounts.GetValueOrDefault(activeTier, 0));

        var visibleTiers = HallOfFameLeague.GetVisibleTierWindow(activeTier)
            .Where(t => tierCounts.GetValueOrDefault(t, 0) > 0)
            .Select(t => MapToTierDto(t, t == activeTier, tierCounts[t]))
            .ToList();

        HallOfFameTargetStudentDto nextTarget = null;
        if (targetEntry != null && targetIndex > 0)
        {
            var aboveEntry = allEntries[targetIndex - 1];
            nextTarget = new HallOfFameTargetStudentDto
            {
                UserId = aboveEntry.UserId,
                DisplayName = aboveEntry.DisplayName,
                Avatar = aboveEntry.Avatar,
                Rank = aboveEntry.Rank,
                Score = aboveEntry.Score,
                XpNeededToOvertake = HallOfFameLeague.CalculateXpToOvertake(targetEntry.Score, aboveEntry.Score),
                TierName = aboveEntry.TierName,
                TierIcon = aboveEntry.TierIcon
            };
        }

        return new HallOfFameLeaderboardDto
        {
            CurrentTier = currentTierDto,
            VisibleTiers = visibleTiers,
            AllTiers = allTiers,
            NextTargetStudent = nextTarget,
            CurrentUserRank = targetEntry?.Rank,
            Entries = allEntries.Where(e => e.Tier == activeTier).ToList(),
            StudentOptions = studentOptions
        };
    }

    private static HallOfFameLeaderboardDto BuildLectorOverviewHallOfFame(
        List<LeaderboardEntryDto> allEntries,
        IReadOnlyList<HallOfFameTierDto> allTiers,
        Dictionary<HallOfFameTier, int> tierCounts,
        IReadOnlyList<HallOfFameStudentOptionDto> studentOptions,
        HallOfFameTier? selectedTier)
    {
        HallOfFameTier focusedTier = selectedTier ?? HallOfFameTier.Obsidian;
        var currentTierDto = MapToTierDto(focusedTier, true, tierCounts.GetValueOrDefault(focusedTier, 0));
        var visibleTiers = HallOfFameLeague.GetVisibleTierWindow(focusedTier)
            .Where(t => tierCounts.GetValueOrDefault(t, 0) > 0)
            .Select(t => MapToTierDto(t, t == focusedTier, tierCounts[t]))
            .ToList();

        var updatedAllTiers = allTiers
            .Select(t => MapToTierDto(t.Tier, selectedTier.HasValue && t.Tier == selectedTier.Value, t.StudentCount))
            .ToList();

        var entries = selectedTier.HasValue
            ? allEntries.Where(e => e.Tier == selectedTier.Value).ToList()
            : allEntries;

        return new HallOfFameLeaderboardDto
        {
            CurrentTier = currentTierDto,
            VisibleTiers = visibleTiers,
            AllTiers = updatedAllTiers,
            NextTargetStudent = null,
            CurrentUserRank = null,
            Entries = entries,
            StudentOptions = studentOptions
        };
    }

    private static HallOfFameTierDto MapToTierDto(HallOfFameTier tier, bool isCurrentTier, int studentCount)
    {
        var meta = HallOfFameLeague.GetTierMetadata(tier);
        return new HallOfFameTierDto
        {
            Tier = tier,
            Name = meta.Name,
            Icon = meta.Icon,
            MinRank = meta.MinRank,
            MaxRank = meta.MaxRank,
            IsCurrentTier = isCurrentTier,
            StudentCount = studentCount
        };
    }

    private static string GetDisplayName(StudentCourseProfile profile)
    {
        string displayName = profile.UsePseudonym
            ? profile.Pseudonym
            : $"{profile.User?.FirstName} {profile.User?.LastName}".Trim();

        return string.IsNullOrWhiteSpace(displayName) ? profile.Pseudonym : displayName;
    }

    public async Task<IReadOnlyList<ClassGroupLeaderboardEntryDto>> GetClassGroupLeaderboardAsync(
        int courseId,
        int? periodId = null,
        int? currentUserId = null)
    {
        var config = await courseGamificationConfigRepository.GetByCourseIdAsync(courseId);
        if (config != null && (!config.IsGamificationEnabled || !config.IsClassCompetitionEnabled))
        {
            return new List<ClassGroupLeaderboardEntryDto>();
        }

        var period = await periodRepository.GetPeriodAsync(periodId);
        var classGroups = await courseClassGroupRepository.GetByCourseAndPeriodAsync(courseId, period.Id);
        var allProfiles = await studentCourseProfileRepository.GetByCourseAndPeriodAsync(courseId, period.Id);

        var standings = ClassGroupCompetition.DetermineStandings(classGroups, allProfiles, currentUserId);

        return standings.Select(s => new ClassGroupLeaderboardEntryDto
        {
            Rank = s.Rank,
            ClassGroupId = s.ClassGroup.Id,
            ClassGroupName = s.ClassGroup.Name,
            StudentCount = s.StudentCount,
            TotalGreenTests = s.TotalGreenTests,
            ClassScore = s.ClassScore,
            TotalXp = s.TotalXp,
            IsCurrentUserClass = s.IsCurrentUserClass,
            CurrentUserContribution = s.CurrentUserContribution
        }).ToList();
    }

    public async Task<CourseGamificationConfigDto> GetCourseConfigAsync(int courseId)
    {
        var config = await courseGamificationConfigRepository.GetByCourseIdAsync(courseId);
        if (config == null)
        {
            config = new CourseGamificationConfig(courseId);
            config = await courseGamificationConfigRepository.AddAsync(config);
        }

        return new CourseGamificationConfigDto
        {
            CourseId = config.CourseId,
            IsGamificationEnabled = config.IsGamificationEnabled,
            IsWeeklyStreakEnabled = config.IsWeeklyStreakEnabled,
            IsLeaderboardEnabled = config.IsLeaderboardEnabled,
            IsClassCompetitionEnabled = config.IsClassCompetitionEnabled,
            IsTeamSynergyEnabled = config.IsTeamSynergyEnabled,
            IsBadgesEnabled = config.IsBadgesEnabled
        };
    }

    public async Task<CourseGamificationConfigDto> UpdateCourseConfigAsync(int courseId, CourseGamificationConfigDto configDto)
    {
        Contracts.Require(configDto != null, "Config data is required.");
        ArgumentNullException.ThrowIfNull(configDto);

        var config = await courseGamificationConfigRepository.GetByCourseIdAsync(courseId);
        if (config == null)
        {
            config = new CourseGamificationConfig(courseId);
            config.UpdateSettings(
                configDto.IsGamificationEnabled,
                configDto.IsWeeklyStreakEnabled,
                configDto.IsLeaderboardEnabled,
                configDto.IsClassCompetitionEnabled,
                configDto.IsTeamSynergyEnabled,
                configDto.IsBadgesEnabled);
            config = await courseGamificationConfigRepository.AddAsync(config);
        }
        else
        {
            config.UpdateSettings(
                configDto.IsGamificationEnabled,
                configDto.IsWeeklyStreakEnabled,
                configDto.IsLeaderboardEnabled,
                configDto.IsClassCompetitionEnabled,
                configDto.IsTeamSynergyEnabled,
                configDto.IsBadgesEnabled);
            config = await courseGamificationConfigRepository.UpdateAsync(config);
        }

        return new CourseGamificationConfigDto
        {
            CourseId = config.CourseId,
            IsGamificationEnabled = config.IsGamificationEnabled,
            IsWeeklyStreakEnabled = config.IsWeeklyStreakEnabled,
            IsLeaderboardEnabled = config.IsLeaderboardEnabled,
            IsClassCompetitionEnabled = config.IsClassCompetitionEnabled,
            IsTeamSynergyEnabled = config.IsTeamSynergyEnabled,
            IsBadgesEnabled = config.IsBadgesEnabled
        };
    }

    public async Task<IReadOnlyList<CourseClassGroupDto>> GetCourseClassGroupsAsync(int courseId, int? periodId = null)
    {
        var period = await periodRepository.GetPeriodAsync(periodId);
        var groups = await courseClassGroupRepository.GetByCourseAndPeriodAsync(courseId, period.Id);

        return groups.Select(g => new CourseClassGroupDto
        {
            Id = g.Id,
            Name = g.Name,
            CourseId = g.CourseId,
            PeriodId = g.PeriodId
        }).ToList();
    }

    public async Task<CourseClassGroupDto> AddCourseClassGroupAsync(int courseId, string name, int? periodId = null)
    {
        Contracts.Require(!string.IsNullOrWhiteSpace(name), "Class group name cannot be empty.");

        var period = await periodRepository.GetPeriodAsync(periodId);
        var existing = await courseClassGroupRepository.GetByNameAsync(courseId, period.Id, name);
        Contracts.Require(existing == null, $"Class group with name '{name}' already exists for this course.");

        var group = courseClassGroupFactory.CreateNew(name, courseId, period.Id);
        group = await courseClassGroupRepository.AddAsync(group);

        return new CourseClassGroupDto
        {
            Id = group.Id,
            Name = group.Name,
            CourseId = group.CourseId,
            PeriodId = group.PeriodId
        };
    }

    public async Task DeleteCourseClassGroupAsync(int courseId, int classGroupId)
    {
        var group = await courseClassGroupRepository.GetByIdAsync(classGroupId);
        if (group.CourseId != courseId)
        {
            throw new DataNotFoundException("Class group not found for this course.");
        }

        await courseClassGroupRepository.DeleteAsync(group);
    }

    public async Task AwardWeeklySprintBadgesAsync(int year, int weekNumber, int? periodId = null)
    {
        var sprintWeek = IsoWeek.From(year, weekNumber);
        DateTime nextMonday = sprintWeek.Next().StartOfWeek;

        var period = await periodRepository.GetPeriodAsync(periodId);
        if (period == null) return;

        var courses = await courseRepository.GetAllAsync();

        foreach (var course in courses)
        {
            var config = await courseGamificationConfigRepository.GetByCourseIdAsync(course.Id);
            await AwardWeeklySprintBadgesForCourseAsync(course, period.Id, sprintWeek, config);

            IReadOnlyList<int> cachedCourseTestIds = null;
            async Task<IReadOnlyList<int>> GetCourseTestIdsAsync()
            {
                if (cachedCourseTestIds == null)
                {
                    cachedCourseTestIds = await assignmentRepository.GetCourseTestIdsAsync(course.Id, period.Id) ?? Array.Empty<int>();
                }
                return cachedCourseTestIds;
            }

            await EvaluateWeeklyRankDefenseForCourseAsync(course.Id, period.Id, sprintWeek, config, GetCourseTestIdsAsync);
            await EvaluateWeeklyStreaksForCourseAsync(course.Id, period.Id, nextMonday, config, GetCourseTestIdsAsync);
        }
    }

    private async Task EvaluateWeeklyRankDefenseForCourseAsync(
        int courseId,
        int periodId,
        IsoWeek sprintWeek,
        CourseGamificationConfig config,
        Func<Task<IReadOnlyList<int>>> getCourseTestIdsAsync)
    {
        if (config == null || !config.IsGamificationEnabled || !config.IsLeaderboardEnabled)
        {
            return;
        }

        var optedInProfiles = await studentCourseProfileRepository.GetForLeaderboardByCourseAndPeriodAsync(courseId, periodId);
        if (optedInProfiles == null || optedInProfiles.Count == 0)
        {
            return;
        }

        IReadOnlyList<int> courseTestIds = await getCourseTestIdsAsync();

        var modifiedProfiles = HallOfFameLeague.EvaluateWeeklyRankDefense(
            optedInProfiles,
            sprintWeek,
            courseTestIds,
            config.IsBadgesEnabled);

        if (modifiedProfiles.Count > 0)
        {
            await studentCourseProfileRepository.UpdateRangeAsync(modifiedProfiles);
        }
    }

    private async Task AwardWeeklySprintBadgesForCourseAsync(
        Course course,
        int periodId,
        IsoWeek sprintWeek,
        CourseGamificationConfig config)
    {
        if (config == null || !config.IsGamificationEnabled)
        {
            return;
        }

        if (!config.IsWeeklyStreakEnabled || !config.IsLeaderboardEnabled || !config.IsBadgesEnabled)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Course {CourseCode} (Id: {CourseId}): 0 weekly sprint badges awarded (gamification enabled, but required sub-feature is disabled).",
                    course.Code, course.Id);
            }
            return;
        }

        var optedInProfiles = await studentCourseProfileRepository.GetForWeeklySprintLeaderboardAsync(course.Id, periodId, sprintWeek.StartOfWeek);
        var sprintEntries = WeeklySprintCompetition.DetermineStandings(optedInProfiles, sprintWeek);

        var (badgesAwarded, modifiedProfiles) = WeeklySprintCompetition.AwardBadges(sprintEntries, sprintWeek);
        foreach (var profile in modifiedProfiles)
        {
            await studentCourseProfileRepository.UpdateAsync(profile);
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Course {CourseCode} (Id: {CourseId}): {BadgeCount} weekly sprint badges awarded.",
                course.Code, course.Id, badgesAwarded);
        }
    }

    private async Task EvaluateWeeklyStreaksForCourseAsync(
        int courseId,
        int periodId,
        DateTime evaluationTimestamp,
        CourseGamificationConfig config,
        Func<Task<IReadOnlyList<int>>> getCourseTestIdsAsync)
    {
        if (config == null || !config.IsGamificationEnabled || !config.IsWeeklyStreakEnabled)
        {
            return;
        }

        var profiles = await studentCourseProfileRepository.GetByCourseAndPeriodAsync(courseId, periodId);
        if (profiles == null || profiles.Count == 0)
        {
            return;
        }

        var candidates = profiles.Where(p => p.NeedsWeeklyStreakEvaluation(evaluationTimestamp)).ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        bool anyCandidateHasPassedTests = candidates.Any(p => p.Events.Any(e => e.EventType == GamificationEventType.PassedTest));
        IReadOnlyList<int> courseTestIds = anyCandidateHasPassedTests
            ? await getCourseTestIdsAsync()
            : Array.Empty<int>();

        var modifiedProfiles = candidates
            .Where(candidate => candidate.EvaluateWeeklyStreak(evaluationTimestamp, courseTestIds))
            .ToList();

        if (modifiedProfiles.Count > 0)
        {
            await studentCourseProfileRepository.UpdateRangeAsync(modifiedProfiles);
        }
    }


    public IReadOnlyList<Badge> GetBadgeMetadata() => Badge.All;

    private static StudentGamificationProfileDto MapToProfileDto(StudentCourseProfile profile)
    {
        DateTime now = DateTime.UtcNow;

        var earnedBadges = profile.Badges.Select(b => new StudentBadgeDto
        {
            Value = b.Badge.Value,
            Name = b.Badge.Name,
            Description = b.Badge.Description,
            Icon = b.Badge.Icon,
            EarnedAt = b.EarnedAt
        }).OrderBy(b => b.EarnedAt).ToList();

        return new StudentGamificationProfileDto
        {
            UserId = profile.UserId,
            FirstName = profile.User?.FirstName,
            LastName = profile.User?.LastName,
            CourseId = profile.CourseId,
            PeriodId = profile.PeriodId,
            ShowInLeaderBoard = profile.ShowInLeaderBoard,
            UsePseudonym = profile.UsePseudonym,
            Pseudonym = profile.Pseudonym,
            Avatar = profile.Avatar,
            ClassGroupId = profile.ClassGroupId,
            ClassGroupName = profile.ClassGroup?.Name,
            TotalXp = profile.TotalXp,
            Level = profile.Level,
            LevelTitle = profile.LevelTitle,
            CurrentWeeklyStreak = profile.WeeklyStreak.CurrentStreak,
            BestWeeklyStreak = profile.WeeklyStreak.BestStreak,
            IsActiveThisWeek = profile.WeeklyStreak.IsActiveInWeek(now),
            TotalGreenTests = profile.TotalGreenTests,
            EarnedBadges = earnedBadges
        };
    }
}
