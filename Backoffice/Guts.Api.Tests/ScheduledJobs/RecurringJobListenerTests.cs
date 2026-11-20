using System;
using System.Threading;
using System.Threading.Tasks;
using Guts.Api.ScheduledJobs;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Quartz;

namespace Guts.Api.Tests.ScheduledJobs;

[TestFixture]
public class RecurringJobListenerTests
{
    private Mock<ILogger<RecurringJobListener>> _loggerMock;
    private Mock<IScheduler> _schedulerMock;
    private FakeTimeProvider _timeProvider;
    private RecurringJobListener _listener;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<RecurringJobListener>>();
        _loggerMock.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        _schedulerMock = new Mock<IScheduler>();
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 11, 20, 10, 0, 0, TimeSpan.Zero));
        _listener = new RecurringJobListener(_loggerMock.Object, _timeProvider);
    }

    [Test]
    public async Task JobLifecycle_WhenSucceeded_ShouldLogAndNotScheduleRetry()
    {
        Assert.That(_listener.Name, Is.EqualTo(nameof(RecurringJobListener)));

        var context = BuildContext("SprintLeaderboard", maxRetries: "3", retryDelaySeconds: "60", retryAttempt: "0");

        await _listener.JobToBeExecuted(context.Object);
        await _listener.JobExecutionVetoed(context.Object);
        await _listener.JobWasExecuted(context.Object, null);

        _schedulerMock.Verify(s => s.ScheduleJob(It.IsAny<ITrigger>(), It.IsAny<ScheduleJobOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task JobWasExecuted_WhenFailedAndRetriesRemaining_ShouldScheduleDelayedRetryTrigger()
    {
        var context = BuildContext("SprintLeaderboard", maxRetries: "3", retryDelaySeconds: "300", retryAttempt: "1");
        ITrigger scheduledTrigger = null;
        _schedulerMock
            .Setup(s => s.ScheduleJob(It.IsAny<ITrigger>(), It.IsAny<ScheduleJobOptions>(), It.IsAny<CancellationToken>()))
            .Callback<ITrigger, ScheduleJobOptions, CancellationToken>((trigger, _, _) => scheduledTrigger = trigger)
            .ReturnsAsync(DateTimeOffset.UtcNow);

        var jobEx = new JobExecutionException("Boom", new InvalidOperationException("Boom"));

        await _listener.JobWasExecuted(context.Object, jobEx);

        Assert.That(scheduledTrigger, Is.Not.Null);
        Assert.That(scheduledTrigger.JobKey, Is.EqualTo(new JobKey("SprintLeaderboard", "GutsRecurring")));
        Assert.That(scheduledTrigger.StartTimeUtc, Is.EqualTo(_timeProvider.GetUtcNow().AddSeconds(300)));
        Assert.That(scheduledTrigger.JobDataMap.GetString(RecurringJobListener.RetryAttemptKey), Is.EqualTo("2"));
    }

    [Test]
    public async Task JobWasExecuted_WhenRetryBudgetExhaustedOrDisabled_ShouldNotScheduleRetry()
    {
        var exhaustedContext = BuildContext("SprintLeaderboard", maxRetries: "3", retryDelaySeconds: "60", retryAttempt: "3");
        var disabledContext = BuildContext("NoRetryJob", maxRetries: "0", retryDelaySeconds: "60", retryAttempt: "0");
        var jobEx = new JobExecutionException("Boom");

        await _listener.JobWasExecuted(exhaustedContext.Object, jobEx);
        await _listener.JobWasExecuted(disabledContext.Object, jobEx);

        _schedulerMock.Verify(s => s.ScheduleJob(It.IsAny<ITrigger>(), It.IsAny<ScheduleJobOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task JobWasExecuted_WhenCancelled_ShouldNotScheduleRetry()
    {
        var context = BuildContext("SprintLeaderboard", maxRetries: "3", retryDelaySeconds: "60", retryAttempt: "0");
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await _listener.JobWasExecuted(context.Object, new JobExecutionException("Cancelled"), cts.Token);
        await _listener.JobWasExecuted(context.Object, new JobExecutionException(new OperationCanceledException()));

        _schedulerMock.Verify(s => s.ScheduleJob(It.IsAny<ITrigger>(), It.IsAny<ScheduleJobOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task JobWasExecuted_WhenJobDataContainsIntOrInvalidString_ShouldHandleSafely()
    {
        var jobData = new JobDataMap
        {
            [RecurringJobListener.MaxRetriesKey] = 2,
            [RecurringJobListener.RetryDelaySecondsKey] = "not-a-number"
        };
        var mergedData = new JobDataMap
        {
            [RecurringJobListener.RetryAttemptKey] = 0
        };

        var jobDetailMock = new Mock<IJobDetail>();
        jobDetailMock.SetupGet(d => d.Key).Returns(new JobKey("TestJob", "GutsRecurring"));
        jobDetailMock.SetupGet(d => d.JobDataMap).Returns(jobData);

        var contextMock = new Mock<IJobExecutionContext>();
        contextMock.SetupGet(c => c.JobDetail).Returns(jobDetailMock.Object);
        contextMock.SetupGet(c => c.MergedJobDataMap).Returns(mergedData);
        contextMock.SetupGet(c => c.Scheduler).Returns(_schedulerMock.Object);

        ITrigger scheduledTrigger = null;
        _schedulerMock
            .Setup(s => s.ScheduleJob(It.IsAny<ITrigger>(), It.IsAny<ScheduleJobOptions>(), It.IsAny<CancellationToken>()))
            .Callback<ITrigger, ScheduleJobOptions, CancellationToken>((trigger, _, _) => scheduledTrigger = trigger)
            .ReturnsAsync(DateTimeOffset.UtcNow);

        await _listener.JobWasExecuted(contextMock.Object, new JobExecutionException("Boom"));

        Assert.That(scheduledTrigger, Is.Not.Null);
        Assert.That(scheduledTrigger.StartTimeUtc, Is.EqualTo(_timeProvider.GetUtcNow().AddSeconds(1)));
    }

    private Mock<IJobExecutionContext> BuildContext(string jobName, string maxRetries, string retryDelaySeconds, string retryAttempt)
    {
        var jobData = new JobDataMap
        {
            [RecurringJobListener.MaxRetriesKey] = maxRetries,
            [RecurringJobListener.RetryDelaySecondsKey] = retryDelaySeconds
        };
        var mergedData = new JobDataMap
        {
            [RecurringJobListener.RetryAttemptKey] = retryAttempt
        };

        var jobDetailMock = new Mock<IJobDetail>();
        jobDetailMock.SetupGet(d => d.Key).Returns(new JobKey(jobName, "GutsRecurring"));
        jobDetailMock.SetupGet(d => d.JobDataMap).Returns(jobData);

        var contextMock = new Mock<IJobExecutionContext>();
        contextMock.SetupGet(c => c.JobDetail).Returns(jobDetailMock.Object);
        contextMock.SetupGet(c => c.MergedJobDataMap).Returns(mergedData);
        contextMock.SetupGet(c => c.Scheduler).Returns(_schedulerMock.Object);
        contextMock.SetupGet(c => c.FireInstanceId).Returns("fire-1");
        contextMock.SetupGet(c => c.JobRunTime).Returns(TimeSpan.FromMilliseconds(42));

        return contextMock;
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
