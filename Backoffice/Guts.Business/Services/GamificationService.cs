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

namespace Guts.Business.Services
{
    public class GamificationService : IGamificationService
    {
        private readonly IStudentCourseProfileRepository _studentCourseProfileRepository;
        private readonly ICourseGamificationConfigRepository _courseGamificationConfigRepository;
        private readonly ICourseClassGroupRepository _courseClassGroupRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly IPeriodRepository _periodRepository;
        private readonly IExamRepository _examRepository;
        private readonly IStudentCourseProfileFactory _studentCourseProfileFactory;
        private readonly ICourseClassGroupFactory _courseClassGroupFactory;
        private readonly ILogger<GamificationService> _logger;

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

        public GamificationService(
            IStudentCourseProfileRepository studentCourseProfileRepository,
            ICourseGamificationConfigRepository courseGamificationConfigRepository,
            ICourseClassGroupRepository courseClassGroupRepository,
            ICourseRepository courseRepository,
            IAssignmentRepository assignmentRepository,
            IPeriodRepository periodRepository,
            IExamRepository examRepository,
            IStudentCourseProfileFactory studentCourseProfileFactory,
            ICourseClassGroupFactory courseClassGroupFactory,
            ILogger<GamificationService> logger)
        {
            _studentCourseProfileRepository = studentCourseProfileRepository;
            _courseGamificationConfigRepository = courseGamificationConfigRepository;
            _courseClassGroupRepository = courseClassGroupRepository;
            _courseRepository = courseRepository;
            _assignmentRepository = assignmentRepository;
            _periodRepository = periodRepository;
            _examRepository = examRepository;
            _studentCourseProfileFactory = studentCourseProfileFactory;
            _courseClassGroupFactory = courseClassGroupFactory;
            _logger = logger;
        }

        public async Task ProcessTestRunAsync(TestRun testRun)
        {
            if (testRun?.TestResults == null || !testRun.TestResults.Any(tr => tr.Passed))
            {
                return;
            }

            try
            {
                var assignment = await _assignmentRepository.GetSingleWithTestsAndCourseAsync(testRun.AssignmentId);
                if (assignment?.Topic == null)
                {
                    return;
                }

                int courseId = assignment.Topic.CourseId;
                int periodId = assignment.Topic.PeriodId;

                // Check course gamification config
                var config = await _courseGamificationConfigRepository.GetByCourseIdAsync(courseId);
                if (config == null || !config.IsGamificationEnabled)
                {
                    return;
                }

                var profile = await _studentCourseProfileRepository.GetByUserCourseAndPeriodAsync(testRun.UserId, courseId, periodId);
                if (profile == null)
                {
                    profile = _studentCourseProfileFactory.CreateNew(testRun.UserId, courseId, periodId);
                    profile = await _studentCourseProfileRepository.AddAsync(profile);
                }

                // Award XP for each passing test
                foreach (var passingResult in testRun.TestResults.Where(tr => tr.Passed))
                {
                    profile.AwardPassedTestXp(passingResult.TestId, testRun.CreateDateTime, config.IsWeeklyStreakEnabled, config.IsBadgesEnabled);
                }

                // Check assignment completion
                var allAssignmentTestIds = assignment.Tests.Select(t => t.Id).ToHashSet();
                var userPassedTestIds = profile.Events
                    .Where(e => e.EventType == GamificationEventType.PassedTest)
                    .Select(e => e.TargetId)
                    .ToHashSet();

                if (allAssignmentTestIds.Count > 0 && allAssignmentTestIds.IsSubsetOf(userPassedTestIds))
                {
                    int previousCompletions = await _studentCourseProfileRepository.CountAssignmentCompletionsAsync(assignment.Id, courseId, periodId);
                    profile.AwardAssignmentBonus(assignment.Id, testRun.CreateDateTime, previousCompletions, config.IsBadgesEnabled);

                    // Check if entire topic / chapter is completed
                    var topicAssignments = await _assignmentRepository.GetByTopicWithTests(assignment.TopicId);
                    var allTopicTestIds = topicAssignments.SelectMany(a => a.Tests).Select(t => t.Id).ToHashSet();
                    if (allTopicTestIds.Count > 0 && allTopicTestIds.IsSubsetOf(userPassedTestIds))
                    {
                        profile.AwardChapterBonus(assignment.TopicId, testRun.CreateDateTime, config.IsBadgesEnabled);
                    }
                }

                await _studentCourseProfileRepository.UpdateAsync(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process gamification for test run {TestRunId}", testRun.Id);
            }
        }

        public async Task<StudentGamificationProfileDto> GetStudentCourseProfileAsync(int userId, int courseId, int? periodId = null)
        {
            var period = await _periodRepository.GetPeriodAsync(periodId);
            var profile = await _studentCourseProfileRepository.GetByUserCourseAndPeriodAsync(userId, courseId, period.Id);

            if (profile == null)
            {
                profile = _studentCourseProfileFactory.CreateNew(userId, courseId, period.Id);
                profile = await _studentCourseProfileRepository.AddAsync(profile);
            }

            if (profile.User == null)
            {
                profile = await _studentCourseProfileRepository.GetByUserCourseAndPeriodAsync(userId, courseId, period.Id) ?? profile;
            }

            var previousVisit = profile.LastLeaderboardVisitDate;

            var recentEvents = previousVisit.HasValue
                ? profile.Events.Where(e => e.CreatedAt > previousVisit.Value).OrderByDescending(e => e.CreatedAt).ToList()
                : profile.Events.OrderByDescending(e => e.CreatedAt).ToList();

            var recentEventDtos = new List<RecentXpEventDto>();
            foreach (var evt in recentEvents)
            {
                string desc = evt.EventType switch
                {
                    GamificationEventType.PassedTest => $"Passed test #{evt.TargetId}",
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

            var recentGains = new RecentXpGainDto
            {
                TotalXpGained = recentEvents.Sum(e => e.XpEarned),
                PreviousVisitDate = previousVisit,
                Events = recentEventDtos
            };

            if (profile.RecordLeaderboardVisit(DateTime.UtcNow))
            {
                await _studentCourseProfileRepository.UpdateAsync(profile);
            }

            var dto = MapToProfileDto(profile);
            dto.LastLeaderboardVisitDate = previousVisit;
            dto.RecentGains = recentGains;
            return dto;
        }

        public async Task<StudentGamificationProfileDto> EnrollStudentAsync(int userId, int courseId, EnrollStudentDto enrollDto, int? periodId = null)
        {
            Contracts.Require(enrollDto != null, "Enrollment data is required.");

            var period = await _periodRepository.GetPeriodAsync(periodId);
            var profile = await _studentCourseProfileRepository.GetByUserCourseAndPeriodAsync(userId, courseId, period.Id);

            if (profile == null)
            {
                profile = _studentCourseProfileFactory.CreateNew(userId, courseId, period.Id);
                profile.Enroll(enrollDto.ShowInLeaderBoard, enrollDto.Pseudonym, enrollDto.Avatar, enrollDto.ClassGroupId, enrollDto.UsePseudonym);
                profile = await _studentCourseProfileRepository.AddAsync(profile);
            }
            else
            {
                profile.Enroll(enrollDto.ShowInLeaderBoard, enrollDto.Pseudonym, enrollDto.Avatar, enrollDto.ClassGroupId, enrollDto.UsePseudonym);
                profile = await _studentCourseProfileRepository.UpdateAsync(profile);
            }

            if (profile.ClassGroupId.HasValue && (profile.ClassGroup == null || profile.ClassGroup.Id != profile.ClassGroupId.Value))
            {
                profile.ClassGroup = await _courseClassGroupRepository.GetByIdAsync(profile.ClassGroupId.Value);
            }
            else if (!profile.ClassGroupId.HasValue)
            {
                profile.ClassGroup = null;
            }

            if (profile.User == null)
            {
                profile = await _studentCourseProfileRepository.GetByUserCourseAndPeriodAsync(userId, courseId, period.Id) ?? profile;
            }

            return MapToProfileDto(profile);
        }

        public async Task<IReadOnlyList<LeaderboardEntryDto>> GetWeeklySprintLeaderboardAsync(int courseId, int currentUserId, int? periodId = null)
        {
            var config = await _courseGamificationConfigRepository.GetByCourseIdAsync(courseId);
            if (config != null && (!config.IsGamificationEnabled || !config.IsWeeklyStreakEnabled || !config.IsLeaderboardEnabled))
            {
                return new List<LeaderboardEntryDto>();
            }

            IPeriod period = await _periodRepository.GetPeriodAsync(periodId);
            IReadOnlyList<StudentCourseProfile> optedInProfiles = await _studentCourseProfileRepository.GetForLeaderboardByCourseAndPeriodAsync(courseId, period.Id);

            return GetWeeklySprintLeaderboard(optedInProfiles, currentUserId, DateTime.Now);
        }

        private IReadOnlyList<LeaderboardEntryDto> GetWeeklySprintLeaderboard(IReadOnlyList<StudentCourseProfile> optedInProfiles, int currentUserId, DateTime timestamp)
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
            var config = await _courseGamificationConfigRepository.GetByCourseIdAsync(courseId);
            if (config != null && (!config.IsGamificationEnabled || !config.IsLeaderboardEnabled))
            {
                return new List<LeaderboardEntryDto>();
            }

            var period = await _periodRepository.GetPeriodAsync(periodId);
            var optedInProfiles = await _studentCourseProfileRepository.GetForLeaderboardByCourseAndPeriodAsync(courseId, period.Id);

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
            var config = await _courseGamificationConfigRepository.GetByCourseIdAsync(courseId);
            if (config != null && (!config.IsGamificationEnabled || !config.IsClassCompetitionEnabled))
            {
                return new List<ClassGroupLeaderboardEntryDto>();
            }

            var period = await _periodRepository.GetPeriodAsync(periodId);
            var classGroups = await _courseClassGroupRepository.GetByCourseAndPeriodAsync(courseId, period.Id);
            var optedInProfiles = await _studentCourseProfileRepository.GetForLeaderboardByCourseAndPeriodAsync(courseId, period.Id);

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
            var config = await _courseGamificationConfigRepository.GetByCourseIdAsync(courseId);
            if (config == null)
            {
                config = new CourseGamificationConfig(courseId);
                config = await _courseGamificationConfigRepository.AddAsync(config);
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

            var config = await _courseGamificationConfigRepository.GetByCourseIdAsync(courseId);
            if (config == null)
            {
                config = new CourseGamificationConfig(courseId);
                config.IsGamificationEnabled = configDto.IsGamificationEnabled;
                config.IsWeeklyStreakEnabled = configDto.IsWeeklyStreakEnabled;
                config.IsLeaderboardEnabled = configDto.IsLeaderboardEnabled;
                config.IsClassCompetitionEnabled = configDto.IsClassCompetitionEnabled;
                config.IsTeamSynergyEnabled = configDto.IsTeamSynergyEnabled;
                config.IsBadgesEnabled = configDto.IsBadgesEnabled;
                config = await _courseGamificationConfigRepository.AddAsync(config);
            }
            else
            {
                config.IsGamificationEnabled = configDto.IsGamificationEnabled;
                config.IsWeeklyStreakEnabled = configDto.IsWeeklyStreakEnabled;
                config.IsLeaderboardEnabled = configDto.IsLeaderboardEnabled;
                config.IsClassCompetitionEnabled = configDto.IsClassCompetitionEnabled;
                config.IsTeamSynergyEnabled = configDto.IsTeamSynergyEnabled;
                config.IsBadgesEnabled = configDto.IsBadgesEnabled;
                config = await _courseGamificationConfigRepository.UpdateAsync(config);
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
            var period = await _periodRepository.GetPeriodAsync(periodId);
            var groups = await _courseClassGroupRepository.GetByCourseAndPeriodAsync(courseId, period.Id);

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

            var period = await _periodRepository.GetPeriodAsync(periodId);
            var existing = await _courseClassGroupRepository.GetByNameAsync(courseId, period.Id, name);
            Contracts.Require(existing == null, $"Class group with name '{name}' already exists for this course.");

            var group = _courseClassGroupFactory.CreateNew(name, courseId, period.Id);
            group = await _courseClassGroupRepository.AddAsync(group);

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
            var group = await _courseClassGroupRepository.GetByIdAsync(classGroupId);
            if (group.CourseId != courseId)
            {
                throw new DataNotFoundException("Class group not found for this course.");
            }

            await _courseClassGroupRepository.DeleteAsync(group);
        }

        public async Task AwardWeeklySprintBadgesAsync(int year, int weekNumber, int? periodId = null)
        {
            DateTime friday = ISOWeek.ToDateTime(year, weekNumber, DayOfWeek.Friday);
            DateTime fridayCutoff = new DateTime(friday.Year, friday.Month, friday.Day, 23, 59, 59, DateTimeKind.Utc);

            var period = await _periodRepository.GetPeriodAsync(periodId);
            if (period == null) return;

            var courses = await _courseRepository.GetAllAsync();

            foreach (var course in courses)
            {
                var config = await _courseGamificationConfigRepository.GetByCourseIdAsync(course.Id);
                if (config != null && (!config.IsGamificationEnabled || !config.IsWeeklyStreakEnabled || !config.IsLeaderboardEnabled || !config.IsBadgesEnabled))
                {
                    continue;
                }

                var optedInProfiles = await _studentCourseProfileRepository.GetForLeaderboardByCourseAndPeriodAsync(course.Id, period.Id);
                var sprintLeaderboard = GetWeeklySprintLeaderboard(optedInProfiles, 0, fridayCutoff);
                var topEntries = sprintLeaderboard.Where(e => e.Score > 0).Take(5).ToList();
                if (topEntries.Count == 0) continue;

                var topEntry = topEntries[0];

                for (int i = 0; i < topEntries.Count; i++)
                {
                    var entry = topEntries[i];
                    int rank = entry.Rank;
                    var profile = optedInProfiles.FirstOrDefault(p => p.UserId == entry.UserId);
                    if (profile == null) continue;

                    bool modified = false;

                    // Award SprintGold to rank 1 or tied for 1st place
                    if (rank == 1 || (entry.Score == topEntry.Score && entry.GreenTestsCount == topEntry.GreenTestsCount))
                    {
                        if (profile.AwardBadge(BadgeType.SprintGold, fridayCutoff))
                        {
                            modified = true;
                        }
                    }

                    if (rank <= 2)
                    {
                        if (profile.AwardBadge(BadgeType.SprintSilver, fridayCutoff))
                        {
                            modified = true;
                        }
                    }

                    if (rank <= 3)
                    {
                        if (profile.AwardBadge(BadgeType.SprintBronze, fridayCutoff))
                        {
                            modified = true;
                        }
                    }

                    if (rank <= 4)
                    {
                        if (profile.AwardBadge(BadgeType.SprintTop4, fridayCutoff))
                        {
                            modified = true;
                        }
                    }

                    if (rank <= 5)
                    {
                        if (profile.AwardBadge(BadgeType.SprintTop5, fridayCutoff))
                        {
                            modified = true;
                        }
                    }

                    if (modified)
                    {
                        await _studentCourseProfileRepository.UpdateAsync(profile);
                    }
                }
            }
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
}
