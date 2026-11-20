using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Guts.Application.Communication;
using Guts.Application.Dtos.Gamification;
using Guts.Application.Repositories;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.CourseAggregate;
using Guts.Domain.GamificationAggregate;
using Guts.Domain.GamificationAggregate.Competitions;
using Guts.Domain.GamificationAggregate.Profiles;
using Guts.Domain.MessageTrackerAggregate;
using Guts.Domain.PeriodAggregate;
using Guts.Domain.TestAggregate;
using Guts.Domain.TopicAggregate;
using Guts.Domain.UserAggregate;
using Guts.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Guts.Application.Services;

public class StreakReminderService : IStreakReminderService
{
    private readonly IPeriodRepository _periodRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly ICourseGamificationConfigRepository _configRepository;
    private readonly ITopicRepository _topicRepository;
    private readonly IStudentCourseProfileRepository _profileRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMessageTrackerRepository _messageTrackerRepository;
    private readonly IMessageTrackerFactory _messageTrackerFactory;
    private readonly IMailSender _mailSender;
    private readonly IMailThrottler _mailThrottler;
    private readonly ILogger<StreakReminderService> _logger;

    public TimeSpan ThrottleDelay
    {
        get => _mailThrottler.ThrottleDelay;
        set => _mailThrottler.ThrottleDelay = value;
    }

    [SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters",
        Justification = "Service coordinates streak reminder workflows across aggregate repositories, message tracking, and mailing infrastructure.")]
    public StreakReminderService(
        IPeriodRepository periodRepository,
        ICourseRepository courseRepository,
        ICourseGamificationConfigRepository configRepository,
        ITopicRepository topicRepository,
        IStudentCourseProfileRepository profileRepository,
        IUserRepository userRepository,
        IMessageTrackerRepository messageTrackerRepository,
        IMessageTrackerFactory messageTrackerFactory,
        IMailSender mailSender,
        IMailThrottler mailThrottler,
        ILogger<StreakReminderService> logger)
    {
        _periodRepository = periodRepository;
        _courseRepository = courseRepository;
        _configRepository = configRepository;
        _topicRepository = topicRepository;
        _profileRepository = profileRepository;
        _userRepository = userRepository;
        _messageTrackerRepository = messageTrackerRepository;
        _messageTrackerFactory = messageTrackerFactory;
        _mailSender = mailSender;
        _mailThrottler = mailThrottler ?? new MailThrottler(TimeSpan.FromSeconds(1));
        _logger = logger;
    }

    public async Task<int> SendStreakRemindersAsync(DateTime executionTime, CancellationToken cancellationToken = default)
    {
        DateTime executionTimeUtc = executionTime.Kind == DateTimeKind.Utc
            ? executionTime
            : executionTime.ToUniversalTime();

        var currentPeriod = await _periodRepository.GetPeriodAsync();
        var allCourses = await _courseRepository.GetAllAsync();

        var currentWeek = IsoWeek.From(executionTimeUtc);

        var messagesToSend = new List<StreakReminderEmailMessage>();
        foreach (var course in allCourses)
        {
            var courseMessages = await BuildMessagesForCourseAsync(course, currentPeriod.Id, currentWeek);
            messagesToSend.AddRange(courseMessages);
        }

        return await SendMessagesAsync(messagesToSend, currentWeek, executionTimeUtc, cancellationToken);
    }

    private async Task<List<StreakReminderEmailMessage>> BuildMessagesForCourseAsync(
        Course course,
        int periodId,
        IsoWeek currentWeek)
    {
        var config = await _configRepository.GetByCourseIdAsync(course.Id);
        if (config == null || !config.IsWeeklyStreakActive)
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
        var eligibleProfiles = await FilterEligibleProfilesAsync(profiles, allCourseTestIds, currentWeek);
        if (eligibleProfiles.Count == 0)
        {
            return new List<StreakReminderEmailMessage>();
        }

        var messages = new List<StreakReminderEmailMessage>();
        foreach (var profile in eligibleProfiles)
        {
            var message = await CreateReminderMessageAsync(profile, course, topics, profiles);
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
            .SelectMany(a => a.GetTestIds())
            .ToHashSet();
    }

    private async Task<List<StudentCourseProfile>> FilterEligibleProfilesAsync(
        IReadOnlyList<StudentCourseProfile> profiles,
        IReadOnlySet<int> allCourseTestIds,
        IsoWeek currentWeek)
    {
        var eligible = new List<StudentCourseProfile>();
        foreach (var profile in profiles)
        {
            if (await _profileRepository.IsUserLectorAsync(profile.UserId))
            {
                continue;
            }

            if (profile.IsStreakAtRisk(currentWeek, allCourseTestIds))
            {
                eligible.Add(profile);
            }
        }

        return eligible;
    }

    private async Task<StreakReminderEmailMessage> CreateReminderMessageAsync(
        StudentCourseProfile profile,
        Course course,
        IReadOnlyList<ITopic> topics,
        IReadOnlyList<StudentCourseProfile> profiles)
    {
        var user = profile.User ?? await _userRepository.GetByIdAsync(profile.UserId);
        if (user == null || string.IsNullOrWhiteSpace(user.Email))
        {
            return null;
        }

        string targetStudentName = await ResolveTargetStudentNameAsync(profile, profiles);
        var passedTestIds = profile.GetPassedTestIds();
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
        IReadOnlyList<StudentCourseProfile> profiles)
    {
        if (!profile.ShowInLeaderBoard)
        {
            return null;
        }

        var targetProfile = HallOfFameLeague.FindCompetitorAbove(profiles, profile.UserId);
        if (targetProfile == null)
        {
            return null;
        }

        User fallbackUser = null;
        if (!targetProfile.UsePseudonym && targetProfile.User == null)
        {
            fallbackUser = await _userRepository.GetByIdAsync(targetProfile.UserId);
        }

        return targetProfile.GetDisplayName(fallbackUser);
    }

    private static List<StreakReminderAssignmentDto> BuildRecommendedAssignments(
        IReadOnlyList<ITopic> topics,
        HashSet<int> passedTestIds)
    {
        var recommendations = AssignmentRecommendationPolicy.RecommendAssignments(topics, passedTestIds, 2);

        return recommendations.Select(r => new StreakReminderAssignmentDto
        {
            AssignmentId = r.AssignmentId,
            AssignmentDescription = r.AssignmentDescription,
            TopicDescription = r.TopicDescription,
            TopicCode = r.TopicCode,
            GreenTestsCount = r.GreenTestsCount,
            TotalTestsCount = r.TotalTestsCount
        }).ToList();
    }

    private async Task<int> SendMessagesAsync(
        List<StreakReminderEmailMessage> messagesToSend,
        IsoWeek currentWeek,
        DateTime executionTimeUtc,
        CancellationToken cancellationToken)
    {
        string entityType = $"StreakReminder:{currentWeek.Year}-W{currentWeek.WeekNumber:D2}";
        int sentCount = 0;

        foreach (var message in messagesToSend)
        {
            cancellationToken.ThrowIfCancellationRequested();

            bool sent = await TrySendReminderMessageAsync(message, entityType, executionTimeUtc, cancellationToken);
            if (sent)
            {
                sentCount++;
            }
        }

        return sentCount;
    }

    private async Task<bool> TrySendReminderMessageAsync(
        StreakReminderEmailMessage message,
        string entityType,
        DateTime executionTimeUtc,
        CancellationToken cancellationToken)
    {
        Email recipientEmail = new Email(message.Recipient.Email);
        var tracker = await _messageTrackerRepository.FindByEntityAndEmailAsync(entityType, message.CourseId, recipientEmail);

        if (tracker != null)
        {
            if (tracker.Status == MessageStatus.Succeeded || !tracker.CanRetry)
            {
                return false;
            }

            tracker.Retry(executionTimeUtc);
            await _messageTrackerRepository.UpdateAsync(tracker);
        }
        else
        {
            tracker = _messageTrackerFactory.CreateNew(entityType, message.CourseId, recipientEmail, executionTimeUtc);
            await _messageTrackerRepository.AddAsync(tracker);
        }

        try
        {
            await _mailSender.SendStreakReminderEmailAsync(
                message.Recipient,
                message.CourseName,
                message.CourseId,
                message.CurrentStreak,
                message.TargetStudentName,
                message.RecommendedAssignments);

            tracker.MarkSucceeded();
            await _messageTrackerRepository.UpdateAsync(tracker);

            await _mailThrottler.ThrottleAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send streak reminder email to user {UserId} for course {CourseId}.", message.Recipient.Id, message.CourseId);
            tracker.MarkFailed(ex.Message);
            await _messageTrackerRepository.UpdateAsync(tracker);
            return false;
        }
    }
}
