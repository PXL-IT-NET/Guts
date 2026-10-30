using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Guts.Business.Communication;
using Guts.Business.Dtos.Gamification;
using Guts.Business.Repositories;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.GamificationAggregate;
using Guts.Domain.TestAggregate;
using Microsoft.Extensions.Logging;

namespace Guts.Business.Services;

public class StreakReminderService : IStreakReminderService
{
    private readonly IPeriodRepository _periodRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly ICourseGamificationConfigRepository _configRepository;
    private readonly ITopicRepository _topicRepository;
    private readonly IStudentCourseProfileRepository _profileRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMailSender _mailSender;
    private readonly ILogger<StreakReminderService> _logger;
    private readonly TimeSpan _throttleDelay;

    public StreakReminderService(
        IPeriodRepository periodRepository,
        ICourseRepository courseRepository,
        ICourseGamificationConfigRepository configRepository,
        ITopicRepository topicRepository,
        IStudentCourseProfileRepository profileRepository,
        IUserRepository userRepository,
        IMailSender mailSender,
        ILogger<StreakReminderService> logger)
        : this(periodRepository, courseRepository, configRepository, topicRepository, profileRepository, userRepository, mailSender, logger, TimeSpan.FromSeconds(1))
    {
    }

    public StreakReminderService(
        IPeriodRepository periodRepository,
        ICourseRepository courseRepository,
        ICourseGamificationConfigRepository configRepository,
        ITopicRepository topicRepository,
        IStudentCourseProfileRepository profileRepository,
        IUserRepository userRepository,
        IMailSender mailSender,
        ILogger<StreakReminderService> logger,
        TimeSpan throttleDelay)
    {
        _periodRepository = periodRepository;
        _courseRepository = courseRepository;
        _configRepository = configRepository;
        _topicRepository = topicRepository;
        _profileRepository = profileRepository;
        _userRepository = userRepository;
        _mailSender = mailSender;
        _logger = logger;
        _throttleDelay = throttleDelay;
    }

    public async Task<int> SendStreakRemindersAsync(DateTime executionTime, CancellationToken cancellationToken = default)
    {
        var currentPeriod = await _periodRepository.GetPeriodAsync();
        var allCourses = await _courseRepository.GetAllAsync();

        int currentYear = ISOWeek.GetYear(executionTime);
        int currentWeek = ISOWeek.GetWeekOfYear(executionTime);

        var messagesToSend = new List<StreakReminderEmailMessage>();

        foreach (var course in allCourses)
        {
            var config = await _configRepository.GetByCourseIdAsync(course.Id);
            if (config == null || !config.IsGamificationEnabled || !config.IsWeeklyStreakEnabled)
            {
                continue;
            }

            var topics = await _topicRepository.GetByCourseWithAssignmentsAndTestsAsync(course.Id, currentPeriod.Id);
            if (topics == null || topics.Count == 0)
            {
                continue;
            }

            var allAssignments = topics
                .SelectMany(t => t.Assignments ?? Enumerable.Empty<Assignment>())
                .ToList();

            var allTests = allAssignments
                .SelectMany(a => a.Tests ?? Enumerable.Empty<Test>())
                .ToList();

            var allCourseTestIds = allTests.Select(t => t.Id).ToHashSet();
            if (allCourseTestIds.Count == 0)
            {
                continue;
            }

            var profiles = await _profileRepository.GetByCourseAndPeriodAsync(course.Id, currentPeriod.Id);

            var eligibleProfiles = new List<StudentCourseProfile>();
            foreach (var profile in profiles)
            {
                if (profile.WeeklyStreak.CurrentStreak <= 0)
                {
                    continue;
                }

                if (await _profileRepository.IsUserLectorAsync(profile.UserId))
                {
                    continue;
                }

                int weeklyXp = profile.Events
                    .Where(e => ISOWeek.GetYear(e.CreatedAt) == currentYear && ISOWeek.GetWeekOfYear(e.CreatedAt) == currentWeek)
                    .Sum(e => e.XpEarned);

                if (weeklyXp > 0)
                {
                    continue;
                }

                var passedTestIds = profile.Events
                    .Where(e => e.EventType == GamificationEventType.PassedTest)
                    .Select(e => e.TargetId)
                    .ToHashSet();

                if (allCourseTestIds.All(passedTestIds.Contains))
                {
                    continue; // All tests completed, streak is protected
                }

                eligibleProfiles.Add(profile);
            }

            if (eligibleProfiles.Count == 0)
            {
                continue;
            }

            var optedInProfiles = profiles
                .Where(p => p.ShowInLeaderBoard)
                .OrderByDescending(p => p.TotalXp)
                .ThenBy(p => p.LastXpEarnedAt)
                .ThenBy(p => p.Id)
                .ToList();

            foreach (var profile in eligibleProfiles)
            {
                var user = profile.User ?? await _userRepository.GetByIdAsync(profile.UserId);
                if (user == null || string.IsNullOrWhiteSpace(user.Email))
                {
                    continue;
                }

                string targetStudentName = null;
                if (profile.ShowInLeaderBoard)
                {
                    int index = optedInProfiles.FindIndex(p => p.UserId == profile.UserId);
                    if (index > 0)
                    {
                        var targetProfile = optedInProfiles[index - 1];
                        targetStudentName = targetProfile.UsePseudonym && !string.IsNullOrWhiteSpace(targetProfile.Pseudonym)
                            ? targetProfile.Pseudonym
                            : $"{targetProfile.User?.FirstName} {targetProfile.User?.LastName}".Trim();

                        if (string.IsNullOrWhiteSpace(targetStudentName))
                        {
                            var targetUser = targetProfile.User ?? await _userRepository.GetByIdAsync(targetProfile.UserId);
                            targetStudentName = $"{targetUser?.FirstName} {targetUser?.LastName}".Trim();
                            if (string.IsNullOrWhiteSpace(targetStudentName))
                            {
                                targetStudentName = targetProfile.Pseudonym;
                            }
                        }
                    }
                }

                var passedTestIds = profile.Events
                    .Where(e => e.EventType == GamificationEventType.PassedTest)
                    .Select(e => e.TargetId)
                    .ToHashSet();

                var inProgress = new List<StreakReminderAssignmentDto>();
                var untouched = new List<StreakReminderAssignmentDto>();

                foreach (var topic in topics)
                {
                    foreach (var assignment in topic.Assignments ?? Enumerable.Empty<Assignment>())
                    {
                        var tests = assignment.Tests ?? new List<Test>();
                        if (tests.Count == 0)
                        {
                            continue;
                        }

                        int passedCount = tests.Count(t => passedTestIds.Contains(t.Id));
                        if (passedCount == tests.Count)
                        {
                            continue; // Completed
                        }

                        var dto = new StreakReminderAssignmentDto
                        {
                            AssignmentId = assignment.Id,
                            AssignmentDescription = assignment.Description,
                            TopicDescription = topic.Description,
                            TopicCode = topic.Code,
                            GreenTestsCount = passedCount,
                            TotalTestsCount = tests.Count
                        };

                        if (passedCount > 0)
                        {
                            inProgress.Add(dto);
                        }
                        else
                        {
                            untouched.Add(dto);
                        }
                    }
                }

                var recommended = inProgress
                    .Concat(untouched)
                    .Take(2)
                    .ToList();

                messagesToSend.Add(new StreakReminderEmailMessage
                {
                    Recipient = user,
                    CourseName = course.Name ?? course.Code,
                    CourseId = course.Id,
                    CurrentStreak = profile.WeeklyStreak.CurrentStreak,
                    TargetStudentName = targetStudentName,
                    RecommendedAssignments = recommended
                });
            }
        }

        int sentCount = 0;
        for (int i = 0; i < messagesToSend.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var message = messagesToSend[i];

            try
            {
                await _mailSender.SendStreakReminderEmailAsync(
                    message.Recipient,
                    message.CourseName,
                    message.CourseId,
                    message.CurrentStreak,
                    message.TargetStudentName,
                    message.RecommendedAssignments);

                sentCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send streak reminder email to user {UserId} for course {CourseId}.", message.Recipient.Id, message.CourseId);
            }

            if (i < messagesToSend.Count - 1 && _throttleDelay > TimeSpan.Zero)
            {
                await Task.Delay(_throttleDelay, cancellationToken);
            }
        }

        return sentCount;
    }
}

