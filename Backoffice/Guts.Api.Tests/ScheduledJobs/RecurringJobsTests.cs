using System;
using System.Threading;
using System.Threading.Tasks;
using Guts.Api.ScheduledJobs;
using Guts.Api.ScheduledJobs.Jobs;
using Guts.Application.Services;
using Guts.Application.Services.Assessment;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Quartz;

namespace Guts.Api.Tests.ScheduledJobs;

[TestFixture]
public class RecurringJobsTests
{
    [Test]
    public async Task SprintLeaderboardJob_Monday0001BrusselsInWinter_ShouldAwardPreviousCompletedIsoWeek()
    {
        // Monday 2026-11-16 00:01 CET (UTC+1) = Sunday 2026-11-15 23:01 UTC!
        // Previous completed ISO week from Monday 2026-11-16 (W47) must be 2026-W46 (NOT W45).
        var gamificationServiceMock = new Mock<IGamificationService>();
        var loggerMock = new Mock<ILogger<SprintLeaderboardJob>>();
        loggerMock.Setup(l => l.IsEnabled(LogLevel.Information)).Returns(true);

        var brusselsTz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Brussels");
        var triggerMock = new Mock<ICronTrigger>();
        triggerMock.SetupGet(t => t.TimeZone).Returns(brusselsTz);

        var contextMock = new Mock<IJobExecutionContext>();
        contextMock.SetupGet(c => c.FireTimeUtc).Returns(new DateTimeOffset(2026, 11, 15, 23, 1, 0, TimeSpan.Zero));
        contextMock.SetupGet(c => c.Trigger).Returns(triggerMock.Object);

        var job = new SprintLeaderboardJob(gamificationServiceMock.Object, loggerMock.Object);

        await job.Execute(contextMock.Object, CancellationToken.None);

        gamificationServiceMock.Verify(s => s.AwardWeeklySprintBadgesAsync(2026, 46, null), Times.Once);
    }

    [Test]
    public async Task SprintLeaderboardJob_WhenTriggerIsNotCronTrigger_ShouldFallbackToBrusselsTimeZone()
    {
        // Monday 2026-07-06 00:01 CEST (UTC+2) = Sunday 2026-07-05 22:01 UTC -> ISO week 2026-W28 -> previous is 2026-W27.
        var gamificationServiceMock = new Mock<IGamificationService>();
        var loggerMock = new Mock<ILogger<SprintLeaderboardJob>>();

        var contextMock = new Mock<IJobExecutionContext>();
        contextMock.SetupGet(c => c.FireTimeUtc).Returns(new DateTimeOffset(2026, 7, 5, 22, 1, 0, TimeSpan.Zero));
        contextMock.SetupGet(c => c.Trigger).Returns(Mock.Of<ITrigger>());

        var job = new SprintLeaderboardJob(gamificationServiceMock.Object, loggerMock.Object);

        await job.Execute(contextMock.Object, CancellationToken.None);

        gamificationServiceMock.Verify(s => s.AwardWeeklySprintBadgesAsync(2026, 27, null), Times.Once);
    }

    [Test]
    public async Task StreakReminderJob_Execute_ShouldDelegateToStreakReminderService()
    {
        var reminderServiceMock = new Mock<IStreakReminderService>();
        var loggerMock = new Mock<ILogger<StreakReminderJob>>();
        loggerMock.Setup(l => l.IsEnabled(LogLevel.Information)).Returns(true);

        var fireTimeUtc = new DateTimeOffset(2026, 11, 19, 11, 0, 0, TimeSpan.Zero);
        using var cts = new CancellationTokenSource();

        var contextMock = new Mock<IJobExecutionContext>();
        contextMock.SetupGet(c => c.FireTimeUtc).Returns(fireTimeUtc);
        reminderServiceMock.Setup(s => s.SendStreakRemindersAsync(fireTimeUtc.UtcDateTime, cts.Token)).ReturnsAsync(5);

        var job = new StreakReminderJob(reminderServiceMock.Object, loggerMock.Object);

        await job.Execute(contextMock.Object, cts.Token);

        reminderServiceMock.Verify(s => s.SendStreakRemindersAsync(fireTimeUtc.UtcDateTime, cts.Token), Times.Once);
    }

    [Test]
    public async Task PeerAssessmentReminderJob_Execute_ShouldDelegateToPeerAssessmentReminderService()
    {
        var reminderServiceMock = new Mock<IPeerAssessmentReminderService>();
        var loggerMock = new Mock<ILogger<PeerAssessmentReminderJob>>();
        loggerMock.Setup(l => l.IsEnabled(LogLevel.Information)).Returns(true);

        var fireTimeUtc = new DateTimeOffset(2026, 11, 19, 14, 0, 0, TimeSpan.Zero);
        using var cts = new CancellationTokenSource();

        var contextMock = new Mock<IJobExecutionContext>();
        contextMock.SetupGet(c => c.FireTimeUtc).Returns(fireTimeUtc);
        reminderServiceMock.Setup(s => s.SendAssessmentRemindersAsync(fireTimeUtc.UtcDateTime, cts.Token)).ReturnsAsync(3);

        var job = new PeerAssessmentReminderJob(reminderServiceMock.Object, loggerMock.Object);

        await job.Execute(contextMock.Object, cts.Token);

        reminderServiceMock.Verify(s => s.SendAssessmentRemindersAsync(fireTimeUtc.UtcDateTime, cts.Token), Times.Once);
    }

    [Test]
    public void RecurringJobDescriptor_FromType_ShouldReadStaticMetadataForAllBuiltInJobs()
    {
        var sprint = RecurringJobDescriptor.FromType(typeof(SprintLeaderboardJob));
        Assert.That(sprint.JobName, Is.EqualTo("SprintLeaderboard"));
        Assert.That(sprint.DefaultCronExpression, Is.EqualTo("0 1 0 ? * MON"));
        Assert.That(sprint.MisfirePolicy, Is.EqualTo(MisfirePolicy.FireAndProceed));
        Assert.That(sprint.MaxRetries, Is.EqualTo(3));
        Assert.That(sprint.RetryDelay, Is.EqualTo(TimeSpan.FromHours(1)));
        Assert.That(sprint.Description, Is.Not.Empty);

        var streak = RecurringJobDescriptor.FromType(typeof(StreakReminderJob));
        Assert.That(streak.JobName, Is.EqualTo("StreakReminder"));
        Assert.That(streak.DefaultCronExpression, Is.EqualTo("0 0 12 ? * THU"));
        Assert.That(streak.MisfirePolicy, Is.EqualTo(MisfirePolicy.FireAndProceed));
        Assert.That(streak.MaxRetries, Is.EqualTo(3));
        Assert.That(streak.RetryDelay, Is.EqualTo(TimeSpan.FromMinutes(15)));

        var peer = RecurringJobDescriptor.FromType(typeof(PeerAssessmentReminderJob));
        Assert.That(peer.JobName, Is.EqualTo("PeerAssessmentReminder"));
        Assert.That(peer.DefaultCronExpression, Is.EqualTo("0 0 */2 ? * *"));
        Assert.That(peer.MisfirePolicy, Is.EqualTo(MisfirePolicy.DoNothing));
        Assert.That(peer.MaxRetries, Is.EqualTo(3));
        Assert.That(peer.RetryDelay, Is.EqualTo(TimeSpan.FromMinutes(15)));
    }

    [Test]
    public void RecurringJobDescriptor_FromType_WhenInvalidType_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => RecurringJobDescriptor.FromType(null));
        Assert.Throws<ArgumentException>(() => RecurringJobDescriptor.FromType(typeof(string)));
        Assert.Throws<ArgumentException>(() => RecurringJobDescriptor.FromType(typeof(IRecurringJob)));
    }
}
