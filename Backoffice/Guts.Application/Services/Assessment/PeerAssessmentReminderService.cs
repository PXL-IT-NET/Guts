using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Guts.Application.Communication;
using Guts.Application.Repositories;
using Guts.Domain.MessageTrackerAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Guts.Domain.UserAggregate;
using Guts.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Guts.Application.Services.Assessment;

public class PeerAssessmentReminderService : IPeerAssessmentReminderService
{
    private readonly IProjectAssessmentRepository _projectAssessmentRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectTeamRepository _projectTeamRepository;
    private readonly IProjectTeamAssessmentService _projectTeamAssessmentService;
    private readonly IMessageTrackerRepository _messageTrackerRepository;
    private readonly IMessageTrackerFactory _messageTrackerFactory;
    private readonly IMailSender _mailSender;
    private readonly IMailThrottler _mailThrottler;
    private readonly ILogger<PeerAssessmentReminderService> _logger;

    [SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters",
        Justification = "Service coordinates reminder discovery, team provisioning, message tracking, and mailing.")]
    public PeerAssessmentReminderService(
        IProjectAssessmentRepository projectAssessmentRepository,
        IProjectRepository projectRepository,
        IProjectTeamRepository projectTeamRepository,
        IProjectTeamAssessmentService projectTeamAssessmentService,
        IMessageTrackerRepository messageTrackerRepository,
        IMessageTrackerFactory messageTrackerFactory,
        IMailSender mailSender,
        IMailThrottler mailThrottler,
        ILogger<PeerAssessmentReminderService> logger)
    {
        _projectAssessmentRepository = projectAssessmentRepository;
        _projectRepository = projectRepository;
        _projectTeamRepository = projectTeamRepository;
        _projectTeamAssessmentService = projectTeamAssessmentService;
        _messageTrackerRepository = messageTrackerRepository;
        _messageTrackerFactory = messageTrackerFactory;
        _mailSender = mailSender;
        _mailThrottler = mailThrottler;
        _logger = logger;
    }

    public async Task<int> SendAssessmentRemindersAsync(DateTime executionTime, CancellationToken cancellationToken = default)
    {
        var openAssessments = await _projectAssessmentRepository.GetOpenAssessmentsAsync(executionTime);
        var eligibleAssessments = openAssessments
            .Where(a => PeerAssessmentReminderPolicy.IsAssessmentEligibleForReminders(a, executionTime))
            .ToList();

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Found {OpenCount} open peer assessment(s); {EligibleCount} eligible for reminders.",
                openAssessments.Count,
                eligibleAssessments.Count);
        }

        if (eligibleAssessments.Count == 0)
        {
            return 0;
        }

        int sentCount = 0;
        foreach (var assessment in eligibleAssessments)
        {
            cancellationToken.ThrowIfCancellationRequested();
            sentCount += await ProcessRemindersForAssessmentAsync(assessment, executionTime, cancellationToken);
        }

        return sentCount;
    }

    private async Task<int> ProcessRemindersForAssessmentAsync(
        IProjectAssessment assessment,
        DateTime executionTime,
        CancellationToken cancellationToken)
    {
        var project = await _projectRepository.LoadWithCourseAsync(assessment.ProjectId);
        if (project == null)
        {
            return 0;
        }

        var teams = await _projectTeamRepository.GetByProjectWithUsersAsync(project.Id);
        if (teams == null || teams.Count == 0)
        {
            return 0;
        }

        string courseName = project.Course?.Name ?? project.Course?.Code ?? "Course";
        int courseId = project.CourseId;
        string projectCode = project.Code?.Value ?? string.Empty;
        string projectDescription = project.Description ?? string.Empty;

        var counts = new ReminderDeliveryCounts();
        foreach (var team in teams)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (team.TeamUsers == null || team.TeamUsers.Count == 0)
            {
                continue;
            }

            var reminderContext = new ReminderContext(
                assessment,
                team.Id,
                courseName,
                courseId,
                projectCode,
                projectDescription,
                executionTime);

            await ProcessTeamRemindersAsync(reminderContext, counts, cancellationToken);
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Assessment {AssessmentId} (Course {CourseName}, Project {ProjectCode}): checked {TeamCount} team(s), {PendingCount} pending evaluator(s) — {SentCount} sent, {SkippedCount} skipped, {FailedCount} failed.",
                assessment.Id,
                courseName,
                projectCode,
                teams.Count,
                counts.PendingEvaluators,
                counts.Sent,
                counts.Skipped,
                counts.Failed);
        }

        return counts.Sent;
    }

    private async Task ProcessTeamRemindersAsync(
        ReminderContext reminderContext,
        ReminderDeliveryCounts counts,
        CancellationToken cancellationToken)
    {
        var teamAssessment = await _projectTeamAssessmentService.GetOrCreateTeamAssessmentAsync(
            reminderContext.Assessment.Id,
            reminderContext.TeamId);
        var pendingPeers = teamAssessment.GetPeersThatNeedToEvaluateOthers();
        counts.PendingEvaluators += pendingPeers.Count;

        foreach (var peer in pendingPeers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(peer.Email))
            {
                counts.Skipped++;
                continue;
            }

            Email peerEmail = new Email(peer.Email);
            SendOutcome outcome = await TrySendReminderToPeerAsync(
                peer,
                peerEmail,
                reminderContext,
                cancellationToken);

            counts.Record(outcome);
        }
    }

    private sealed class ReminderDeliveryCounts
    {
        public int PendingEvaluators { get; set; }
        public int Sent { get; private set; }
        public int Skipped { get; set; }
        public int Failed { get; private set; }

        public void Record(SendOutcome outcome)
        {
            switch (outcome)
            {
                case SendOutcome.Sent:
                    Sent++;
                    break;
                case SendOutcome.Skipped:
                    Skipped++;
                    break;
                case SendOutcome.Failed:
                    Failed++;
                    break;
            }
        }
    }

    private enum SendOutcome
    {
        Sent,
        Skipped,
        Failed
    }

    private async Task<SendOutcome> TrySendReminderToPeerAsync(
        User peer,
        Email peerEmail,
        ReminderContext context,
        CancellationToken cancellationToken)
    {
        var tracker = await _messageTrackerRepository.FindByEntityAndEmailAsync("PeerAssessment", context.Assessment.Id, peerEmail);

        if (tracker != null)
        {
            if (tracker.Status == MessageStatus.Succeeded || !tracker.CanRetry)
            {
                return SendOutcome.Skipped;
            }

            tracker.Retry(context.ExecutionTime);
            await _messageTrackerRepository.UpdateAsync(tracker);
        }
        else
        {
            tracker = _messageTrackerFactory.CreateNew("PeerAssessment", context.Assessment.Id, peerEmail, context.ExecutionTime);
            await _messageTrackerRepository.AddAsync(tracker);
        }

        try
        {
            await _mailSender.SendPeerAssessmentReminderEmailAsync(
                peer,
                context.CourseName,
                context.CourseId,
                context.ProjectCode,
                context.ProjectDescription,
                context.Assessment,
                context.TeamId);

            tracker.MarkSucceeded();
            await _messageTrackerRepository.UpdateAsync(tracker);

            await _mailThrottler.ThrottleAsync(cancellationToken);
            return SendOutcome.Sent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send peer assessment reminder email to {Email} for assessment {AssessmentId}.", peer.Email, context.Assessment.Id);
            tracker.MarkFailed(ex.Message);
            await _messageTrackerRepository.UpdateAsync(tracker);
            return SendOutcome.Failed;
        }
    }

    private sealed record ReminderContext(
        IProjectAssessment Assessment,
        int TeamId,
        string CourseName,
        int CourseId,
        string ProjectCode,
        string ProjectDescription,
        DateTime ExecutionTime);
}
