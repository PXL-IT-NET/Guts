using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Guts.Business.Communication;
using Guts.Business.Dtos.Gamification;
using Guts.Business.Repositories;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.CourseAggregate;
using Guts.Domain.GamificationAggregate;
using Guts.Domain.PeriodAggregate;
using Guts.Domain.TestAggregate;
using Guts.Domain.TopicAggregate;
using Guts.Domain.UserAggregate;
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

    public TimeSpan ThrottleDelay { get; set; } = TimeSpan.FromSeconds(1);

    [SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters",
        Justification = "Service coordinates streak reminder workflows across aggregate repositories and mailing infrastructure.")]
    public StreakReminderService(
        IPeriodRepository periodRepository,
        ICourseRepository courseRepository,
        ICourseGamificationConfigRepository configRepository,
        ITopicRepository topicRepository,
        IStudentCourseProfileRepository profileRepository,
        IUserRepository userRepository,
        IMailSender mailSender,
        ILogger<StreakReminderService> logger)
    {
        _periodRepository = periodRepository;
        _courseRepository = courseRepository;
        _configRepository = configRepository;
        _topicRepository = topicRepository;
        _profileRepository = profileRepository;
        _userRepository = userRepository;
        _mailSender = mailSender;
        _logger = logger;
    }

    [SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters",
        Justification = "Overload constructor enabling custom throttle delay for testing.")]
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
        : this(periodRepository, courseRepository, configRepository, topicRepository, profileRepository, userRepository, mailSender, logger)
    {
        ThrottleDelay = throttleDelay;
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
            var courseMessages = await BuildMessagesForCourseAsync(course, currentPeriod.Id, currentYear, currentWeek);
            messagesToSend.AddRange(courseMessages);
        }

        return await SendMessagesAsync(messagesToSend, cancellationToken);
    }

    private async Task<List<StreakReminderEmailMessage>> BuildMessagesForCourseAsync(
        Course course,
        int periodId,
        int currentYear,
        int currentWeek)
    {
        var config = await _configRepository.GetByCourseIdAsync(course.Id);
        if (config == null || !config.IsGamificationEnabled || !config.IsWeeklyStreakEnabled)
        {
            return new List<StreakReminderEmailMessage>();
        }

        var topics = await _topicRepository.GetByCourseWithAssignmentsAndTestsAsync(course.Id, periodId);
        if (topics == null || topics.Count == 0)
        {
            return new List<StreakReminderEmailMessage>();
        }

        var allCourseTestIds = GetAllCourseTestIds(topics);
        if (allCourseTestIds.Count == 0)
        {
            return new List<StreakReminderEmailMessage>();
        }

        var profiles = await _profileRepository.GetByCourseAndPeriodAsync(course.Id, periodId);
        var eligibleProfiles = await FilterEligibleProfilesAsync(profiles, allCourseTestIds, currentYear, currentWeek);
        if (eligibleProfiles.Count == 0)
        {
            return new List<StreakReminderEmailMessage>();
        }

        var optedInProfiles = GetOptedInLeaderboardProfiles(profiles);
        var messages = new List<StreakReminderEmailMessage>();
        foreach (var profile in eligibleProfiles)
        {
            var message = await CreateReminderMessageAsync(profile, course, topics, optedInProfiles);
            if (message != null)
            {
                messages.Add(message);
            }
        }

        return messages;
    }

    private static HashSet<int> GetAllCourseTestIds(IReadOnlyList<ITopic> topics)
    {
        return topics
            .SelectMany(t => t.Assignments ?? Enumerable.Empty<Assignment>())
            .SelectMany(a => a.Tests ?? Enumerable.Empty<Test>())
            .Select(t => t.Id)
            .ToHashSet();
    }

    private async Task<List<StudentCourseProfile>> FilterEligibleProfilesAsync(
        IReadOnlyList<StudentCourseProfile> profiles,
        HashSet<int> allCourseTestIds,
        int currentYear,
        int currentWeek)
    {
        var eligible = new List<StudentCourseProfile>();
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

            var passedTestIds = GetPassedTestIds(profile);
            if (allCourseTestIds.All(passedTestIds.Contains))
            {
                continue;
            }

            eligible.Add(profile);
        }

        return eligible;
    }

    private static HashSet<int> GetPassedTestIds(StudentCourseProfile profile)
    {
        return profile.Events
            .Where(e => e.EventType == GamificationEventType.PassedTest)
            .Select(e => e.TargetId)
            .ToHashSet();
    }

    private static List<StudentCourseProfile> GetOptedInLeaderboardProfiles(IReadOnlyList<StudentCourseProfile> profiles)
    {
        return profiles
            .Where(p => p.ShowInLeaderBoard)
            .OrderByDescending(p => p.TotalXp)
            .ThenBy(p => p.LastXpEarnedAt)
            .ThenBy(p => p.Id)
            .ToList();
    }

    private async Task<StreakReminderEmailMessage> CreateReminderMessageAsync(
        StudentCourseProfile profile,
        Course course,
        IReadOnlyList<ITopic> topics,
        List<StudentCourseProfile> optedInProfiles)
    {
        var user = profile.User ?? await _userRepository.GetByIdAsync(profile.UserId);
        if (user == null || string.IsNullOrWhiteSpace(user.Email))
        {
            return null;
        }

        string targetStudentName = await ResolveTargetStudentNameAsync(profile, optedInProfiles);
        var passedTestIds = GetPassedTestIds(profile);
        var recommended = BuildRecommendedAssignments(topics, passedTestIds);

        return new StreakReminderEmailMessage
        {
            Recipient = user,
            CourseName = course.Name ?? course.Code,
            CourseId = course.Id,
            CurrentStreak = profile.WeeklyStreak.CurrentStreak,
            TargetStudentName = targetStudentName,
            RecommendedAssignments = recommended
        };
    }

    private async Task<string> ResolveTargetStudentNameAsync(
        StudentCourseProfile profile,
        List<StudentCourseProfile> optedInProfiles)
    {
        if (!profile.ShowInLeaderBoard)
        {
            return null;
        }

        int index = optedInProfiles.FindIndex(p => p.UserId == profile.UserId);
        if (index <= 0)
        {
            return null;
        }

        var targetProfile = optedInProfiles[index - 1];
        string targetStudentName = targetProfile.UsePseudonym && !string.IsNullOrWhiteSpace(targetProfile.Pseudonym)
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

        return targetStudentName;
    }

    private static List<StreakReminderAssignmentDto> BuildRecommendedAssignments(
        IReadOnlyList<ITopic> topics,
        HashSet<int> passedTestIds)
    {
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
                    continue;
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

        return inProgress.Concat(untouched).Take(2).ToList();
    }

    private async Task<int> SendMessagesAsync(
        List<StreakReminderEmailMessage> messagesToSend,
        CancellationToken cancellationToken)
    {
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

            if (i < messagesToSend.Count - 1 && ThrottleDelay > TimeSpan.Zero)
            {
                await Task.Delay(ThrottleDelay, cancellationToken);
            }
        }

        return sentCount;
    }
}
