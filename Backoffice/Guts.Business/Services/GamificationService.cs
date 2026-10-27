using Guts.Domain.AssignmentAggregate;
using System.Diagnostics.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Guts.Business.Dtos.Gamification;
using Guts.Business.Repositories;
using Guts.Common;
using Guts.Domain.GamificationAggregate;
using Guts.Domain.PeriodAggregate;
using Guts.Domain.TestRunAggregate;
using Microsoft.Extensions.Logging;

namespace Guts.Business.Services;

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
    private static readonly Dictionary<BadgeType, (string Name, string Description, string Icon)> BadgeMetadata = new()
    {
        { BadgeType.FirstSpark, ("First Spark", "Passed your first test in the course", "🌱") },
        { BadgeType.SpeedDemon, ("Speed Demon", "Completed all tests of an assignment within 24 hours", "⚡") },
        { BadgeType.CleanSweep, ("Clean Sweep", "Solved 100% of all exercises in a chapter", "🧹") },
        { BadgeType.NightOwl, ("Night Owl", "Submitted a passing test between 00:00 and 05:00", "🦉") },
        { BadgeType.EarlyBird, ("Early Bird", "Submitted a passing test between 05:00 and 08:00", "🌅") },
        { BadgeType.WeekendWarrior, ("Weekend Warrior", "Passed a test during the weekend", "⚔️") },
        { BadgeType.CenturyClub, ("Century Club", "Achieved 100 unique passing tests in the course", "💯") },
        { BadgeType.OnFire, ("On Fire", "Maintained a 4-week coding streak", "🔥") },
        { BadgeType.IronWill, ("Iron Will", "Maintained an 8-week coding streak", "🛡️") },
        { BadgeType.SprintGold, ("Sprint Gold", "Finished #1 on the Weekly Sprint Leaderboard", "🥇") },
        { BadgeType.SprintSilver, ("Sprint Silver", "Finished in the top 2 of the Weekly Sprint Leaderboard", "🥈") },
        { BadgeType.SprintBronze, ("Sprint Bronze", "Finished in the top 3 of the Weekly Sprint Leaderboard", "🥉") },
        { BadgeType.SprintTop4, ("Sprint Top 4", "Finished in the top 4 of the Weekly Sprint Leaderboard", "🎖️") },
        { BadgeType.SprintTop5, ("Sprint Top 5", "Finished in the top 5 of the Weekly Sprint Leaderboard", "⭐") },
        { BadgeType.TeamPowerhouse, ("Team Powerhouse", "Project team where all members have green tests", "🤝") }
    };

    public async Task ProcessTestRunAsync(TestRun testRun)
    {
        if (testRun?.TestResults == null || !testRun.TestResults.Any(tr => tr.Passed))
        {
            return;
        }

        try
        {
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

            AwardPassedTestsXp(profile, testRun, config);

            var allAssignmentTestIds = assignment.Tests.Select(t => t.Id).ToHashSet();
            var userPassedTestIds = profile.Events
                .Where(e => e.EventType == GamificationEventType.PassedTest)
                .Select(e => e.TargetId)
                .ToHashSet();

            bool allTestsOfAssignmentPassed = await CheckAssignmentAndTopicCompletionAsync(
                profile, assignment, courseId, periodId, testRun.CreateDateTime, config, userPassedTestIds, allAssignmentTestIds);

            if (config.IsTeamSynergyEnabled && allTestsOfAssignmentPassed)
            {
                await CheckTeamSynergyBonusAsync(profile, assignment, allAssignmentTestIds, courseId, periodId, testRun, config);
            }

            await studentCourseProfileRepository.UpdateAsync(profile);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process gamification for test run {TestRunId}", testRun.Id);
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
        HashSet<int> userPassedTestIds,
        HashSet<int> allAssignmentTestIds)
    {
        bool allTestsOfAssignmentPassed = allAssignmentTestIds.Count > 0 && allAssignmentTestIds.IsSubsetOf(userPassedTestIds);
        if (allTestsOfAssignmentPassed)
        {
            int previousCompletions = await studentCourseProfileRepository.CountAssignmentCompletionsAsync(assignment.Id, courseId, periodId);
            profile.AwardAssignmentBonus(assignment.Id, testRunDateTime, previousCompletions, config.IsBadgesEnabled);

            var topicAssignments = await assignmentRepository.GetByTopicWithTests(assignment.TopicId);
            var allTopicTestIds = topicAssignments.SelectMany(a => a.Tests).Select(t => t.Id).ToHashSet();
            if (allTopicTestIds.Count > 0 && allTopicTestIds.IsSubsetOf(userPassedTestIds))
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

        bool allMembersContributed = teamProfiles.Count == teamUserIds.Count && teamProfiles.All(p =>
        {
            var memberPassedTests = p.Events
                .Where(e => e.EventType == GamificationEventType.PassedTest)
                .Select(e => e.TargetId);
            return allAssignmentTestIds.Overlaps(memberPassedTests);
        });

        if (!allMembersContributed) return;

        var allTeamPassedTests = teamProfiles
            .SelectMany(p => p.Events
                .Where(e => e.EventType == GamificationEventType.PassedTest)
                .Select(e => e.TargetId))
            .ToHashSet();

        if (!allAssignmentTestIds.IsSubsetOf(allTeamPassedTests)) return;

        foreach (var memberProfile in teamProfiles.Where(p => p.AwardTeamSynergyBonus(assignment.Id, testRun.CreateDateTime, config.IsBadgesEnabled)))
        {
            if (memberProfile.UserId != testRun.UserId)
            {
                await studentCourseProfileRepository.UpdateAsync(memberProfile);
            }
        }
    }
    public async Task<StudentGamificationProfileDto> GetStudentCourseProfileAsync(int userId, int courseId, int? periodId = null)
    {
        var period = await periodRepository.GetPeriodAsync(periodId);
        var profile = await studentCourseProfileRepository.GetByUserCourseAndPeriodAsync(userId, courseId, period.Id);

        if (profile == null)
        {
            profile = studentCourseProfileFactory.CreateNew(userId, courseId, period.Id);
            profile = await studentCourseProfileRepository.AddAsync(profile);
        }

        if (profile.User == null)
        {
            profile = await studentCourseProfileRepository.GetByUserCourseAndPeriodAsync(userId, courseId, period.Id) ?? profile;
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
        bool visitUpdated = profile.RecordLeaderboardVisit(now);

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

        foreach (var evt in recentEvents.Where(e => e.EventType != GamificationEventType.PassedTest))
        {
            string desc = evt.EventType switch
            {
                GamificationEventType.AssignmentCompleted => "Completed assignment bonus",
                GamificationEventType.AssignmentCompletedQuickly => "Quick assignment completion bonus (Top 10)",
                GamificationEventType.ChapterCompleted => "Completed chapter bonus",
                GamificationEventType.TeamSynergy => "Team synergy bonus",
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
            profile.ClassGroup = await courseClassGroupRepository.GetByIdAsync(profile.ClassGroupId.Value);
        }
        else if (!profile.ClassGroupId.HasValue)
        {
            profile.ClassGroup = null;
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
        IReadOnlyList<StudentCourseProfile> optedInProfiles = await studentCourseProfileRepository.GetForLeaderboardByCourseAndPeriodAsync(courseId, period.Id);

        return GetWeeklySprintLeaderboard(optedInProfiles, currentUserId, DateTime.Now);
    }

    private static List<LeaderboardEntryDto> GetWeeklySprintLeaderboard(IReadOnlyList<StudentCourseProfile> optedInProfiles, int currentUserId, DateTime timestamp)
    {
        DateTime startOfWeek = ISOWeek.ToDateTime(ISOWeek.GetYear(timestamp), ISOWeek.GetWeekOfYear(timestamp), DayOfWeek.Monday);

        var scored = optedInProfiles.Select(profile =>
            {
                var weeklyEvents = profile.Events.Where(e => e.CreatedAt >= startOfWeek && e.CreatedAt <= timestamp).ToList();
                int weekXp = weeklyEvents.Sum(e => e.XpEarned);
                int greenTests = weeklyEvents.Count(e => e.EventType == GamificationEventType.PassedTest);

                return new
                {
                    Profile = profile,
                    Score = weekXp,
                    GreenTests = greenTests
                };
            })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.GreenTests)
            .ToList();

        var result = new List<LeaderboardEntryDto>();
        for (int i = 0; i < scored.Count; i++)
        {
            var item = scored[i];
            string displayName = item.Profile.UsePseudonym
                ? item.Profile.Pseudonym
                : $"{item.Profile.User?.FirstName} {item.Profile.User?.LastName}".Trim();

            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = item.Profile.Pseudonym;
            }

            result.Add(new LeaderboardEntryDto
            {
                Rank = i + 1,
                UserId = item.Profile.UserId,
                DisplayName = displayName,
                Avatar = item.Profile.Avatar,
                ClassGroupName = item.Profile.ClassGroup?.Name ?? "Unassigned",
                Score = item.Score,
                Level = item.Profile.Level,
                WeeklyStreak = item.Profile.WeeklyStreak.CurrentStreak,
                GreenTestsCount = item.GreenTests,
                IsCurrentUser = item.Profile.UserId == currentUserId
            });
        }

        return result;
    }

    public async Task<IReadOnlyList<LeaderboardEntryDto>> GetHallOfFameLeaderboardAsync(int courseId, int currentUserId, int? periodId = null)
    {
        var config = await courseGamificationConfigRepository.GetByCourseIdAsync(courseId);
        if (config != null && (!config.IsGamificationEnabled || !config.IsLeaderboardEnabled))
        {
            return new List<LeaderboardEntryDto>();
        }

        var period = await periodRepository.GetPeriodAsync(periodId);
        var optedInProfiles = await studentCourseProfileRepository.GetForLeaderboardByCourseAndPeriodAsync(courseId, period.Id);

        var ordered = optedInProfiles
            .OrderByDescending(p => p.TotalXp)
            .ThenByDescending(p => p.Events.Count(e => e.EventType == GamificationEventType.PassedTest))
            .ToList();

        var result = new List<LeaderboardEntryDto>();
        for (int i = 0; i < ordered.Count; i++)
        {
            var profile = ordered[i];
            string displayName = profile.UsePseudonym
                ? profile.Pseudonym
                : $"{profile.User?.FirstName} {profile.User?.LastName}".Trim();

            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = profile.Pseudonym;
            }

            int totalGreen = profile.Events.Count(e => e.EventType == GamificationEventType.PassedTest);

            result.Add(new LeaderboardEntryDto
            {
                Rank = i + 1,
                UserId = profile.UserId,
                DisplayName = displayName,
                Avatar = profile.Avatar,
                ClassGroupName = profile.ClassGroup?.Name ?? "Unassigned",
                Score = profile.TotalXp,
                Level = profile.Level,
                WeeklyStreak = profile.WeeklyStreak.CurrentStreak,
                GreenTestsCount = totalGreen,
                IsCurrentUser = profile.UserId == currentUserId
            });
        }

        return result;
    }

    public async Task<IReadOnlyList<ClassGroupLeaderboardEntryDto>> GetClassGroupLeaderboardAsync(int courseId, int? periodId = null)
    {
        var config = await courseGamificationConfigRepository.GetByCourseIdAsync(courseId);
        if (config != null && (!config.IsGamificationEnabled || !config.IsClassCompetitionEnabled))
        {
            return new List<ClassGroupLeaderboardEntryDto>();
        }

        var period = await periodRepository.GetPeriodAsync(periodId);
        var classGroups = await courseClassGroupRepository.GetByCourseAndPeriodAsync(courseId, period.Id);
        var optedInProfiles = await studentCourseProfileRepository.GetForLeaderboardByCourseAndPeriodAsync(courseId, period.Id);

        var result = new List<ClassGroupLeaderboardEntryDto>();

        foreach (var group in classGroups)
        {
            var groupStudents = optedInProfiles.Where(p => p.ClassGroupId == group.Id).ToList();
            int studentCount = groupStudents.Count;
            int totalXp = groupStudents.Sum(p => p.TotalXp);
            int totalGreen = groupStudents.Sum(p => p.Events.Count(e => e.EventType == GamificationEventType.PassedTest));
            double avgXp = studentCount > 0 ? Math.Round((double)totalXp / studentCount, 1) : 0;

            result.Add(new ClassGroupLeaderboardEntryDto
            {
                ClassGroupId = group.Id,
                ClassGroupName = group.Name,
                StudentCount = studentCount,
                TotalGreenTests = totalGreen,
                AverageXp = avgXp,
                TotalXp = totalXp
            });
        }

        var ordered = result.OrderByDescending(r => r.AverageXp).ThenByDescending(r => r.TotalGreenTests).ToList();
        for (int i = 0; i < ordered.Count; i++)
        {
            ordered[i].Rank = i + 1;
        }

        return ordered;
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
            config.IsGamificationEnabled = configDto.IsGamificationEnabled;
            config.IsWeeklyStreakEnabled = configDto.IsWeeklyStreakEnabled;
            config.IsLeaderboardEnabled = configDto.IsLeaderboardEnabled;
            config.IsClassCompetitionEnabled = configDto.IsClassCompetitionEnabled;
            config.IsTeamSynergyEnabled = configDto.IsTeamSynergyEnabled;
            config.IsBadgesEnabled = configDto.IsBadgesEnabled;
            config = await courseGamificationConfigRepository.AddAsync(config);
        }
        else
        {
            config.IsGamificationEnabled = configDto.IsGamificationEnabled;
            config.IsWeeklyStreakEnabled = configDto.IsWeeklyStreakEnabled;
            config.IsLeaderboardEnabled = configDto.IsLeaderboardEnabled;
            config.IsClassCompetitionEnabled = configDto.IsClassCompetitionEnabled;
            config.IsTeamSynergyEnabled = configDto.IsTeamSynergyEnabled;
            config.IsBadgesEnabled = configDto.IsBadgesEnabled;
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
        DateTime friday = ISOWeek.ToDateTime(year, weekNumber, DayOfWeek.Friday);
        DateTime fridayCutoff = new DateTime(friday.Year, friday.Month, friday.Day, 23, 59, 59, DateTimeKind.Utc);
        DateTime nextMonday = ISOWeek.ToDateTime(year, weekNumber, DayOfWeek.Monday).AddDays(7);

        var period = await periodRepository.GetPeriodAsync(periodId);
        if (period == null) return;

        var courses = await courseRepository.GetAllAsync();

        foreach (int courseId in courses.Select(course => course.Id))
        {
            var config = await courseGamificationConfigRepository.GetByCourseIdAsync(courseId);
            await AwardWeeklySprintBadgesForCourseAsync(courseId, period.Id, fridayCutoff, config);
            await EvaluateWeeklyStreaksForCourseAsync(courseId, period.Id, nextMonday, config);
        }
    }

    private async Task AwardWeeklySprintBadgesForCourseAsync(
        int courseId,
        int periodId,
        DateTime fridayCutoff,
        CourseGamificationConfig config)
    {
        if (config != null && (!config.IsGamificationEnabled || !config.IsWeeklyStreakEnabled || !config.IsLeaderboardEnabled || !config.IsBadgesEnabled))
        {
            return;
        }

        var optedInProfiles = await studentCourseProfileRepository.GetForLeaderboardByCourseAndPeriodAsync(courseId, periodId);
        var sprintLeaderboard = GetWeeklySprintLeaderboard(optedInProfiles, 0, fridayCutoff);
        var topEntries = sprintLeaderboard.Where(e => e.Score > 0).Take(5).ToList();
        if (topEntries.Count == 0) return;

        var topEntry = topEntries[0];

        foreach (var entry in topEntries)
        {
            var profile = optedInProfiles.FirstOrDefault(p => p.UserId == entry.UserId);
            if (profile != null && AwardSprintBadgesForEntry(profile, entry, topEntry, fridayCutoff))
            {
                await studentCourseProfileRepository.UpdateAsync(profile);
            }
        }
    }

    private async Task EvaluateWeeklyStreaksForCourseAsync(
        int courseId,
        int periodId,
        DateTime evaluationTimestamp,
        CourseGamificationConfig config)
    {
        if (config != null && (!config.IsGamificationEnabled || !config.IsWeeklyStreakEnabled))
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
            ? await assignmentRepository.GetCourseTestIdsAsync(courseId, periodId)
            : Array.Empty<int>();

        var modifiedProfiles = candidates
            .Where(candidate => candidate.EvaluateWeeklyStreak(evaluationTimestamp, courseTestIds))
            .ToList();

        if (modifiedProfiles.Count > 0)
        {
            await studentCourseProfileRepository.UpdateRangeAsync(modifiedProfiles);
        }
    }

    private static bool AwardSprintBadgesForEntry(
        StudentCourseProfile profile,
        LeaderboardEntryDto entry,
        LeaderboardEntryDto topEntry,
        DateTime fridayCutoff)
    {
        bool modified = false;
        int rank = entry.Rank;

        if ((rank == 1 || (entry.Score == topEntry.Score && entry.GreenTestsCount == topEntry.GreenTestsCount)) &&
            profile.AwardBadge(BadgeType.SprintGold, fridayCutoff))
        {
            modified = true;
        }

        if (rank <= 2 && profile.AwardBadge(BadgeType.SprintSilver, fridayCutoff))
        {
            modified = true;
        }

        if (rank <= 3 && profile.AwardBadge(BadgeType.SprintBronze, fridayCutoff))
        {
            modified = true;
        }

        if (rank <= 4 && profile.AwardBadge(BadgeType.SprintTop4, fridayCutoff))
        {
            modified = true;
        }

        if (rank <= 5 && profile.AwardBadge(BadgeType.SprintTop5, fridayCutoff))
        {
            modified = true;
        }

        return modified;
    }
    public IReadOnlyList<BadgeMetadataDto> GetBadgeMetadata()
    {
        return BadgeMetadata.Select(kvp => new BadgeMetadataDto
        {
            BadgeType = kvp.Key,
            Name = kvp.Value.Name,
            Description = kvp.Value.Description,
            Icon = kvp.Value.Icon
        }).ToList();
    }

    private static StudentGamificationProfileDto MapToProfileDto(StudentCourseProfile profile)
    {
        DateTime now = DateTime.UtcNow;
        int totalGreen = profile.Events.Count(e => e.EventType == GamificationEventType.PassedTest);

        var earnedBadges = profile.Badges.Select(b =>
        {
            BadgeMetadata.TryGetValue(b.BadgeType, out var meta);
            return new StudentBadgeDto
            {
                BadgeType = b.BadgeType,
                Name = meta.Name ?? b.BadgeType.ToString(),
                Description = meta.Description ?? string.Empty,
                Icon = meta.Icon ?? "🏅",
                EarnedAt = b.EarnedAt
            };
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
            TotalGreenTests = totalGreen,
            EarnedBadges = earnedBadges
        };
    }
}