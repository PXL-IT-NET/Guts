using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Guts.Api.ScheduledJobs;
using Guts.Api.ScheduledJobs.Jobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Quartz;

namespace Guts.Api.Tests.ScheduledJobs;

[TestFixture]
public class ScheduledJobsSyncHostedServiceTests
{
    private Mock<ISchedulerFactory> _schedulerFactoryMock;
    private Mock<IScheduler> _schedulerMock;
    private Mock<ILogger<ScheduledJobsSyncHostedService>> _loggerMock;

    [SetUp]
    public void SetUp()
    {
        _schedulerFactoryMock = new Mock<ISchedulerFactory>();
        _schedulerMock = new Mock<IScheduler>();
        _loggerMock = new Mock<ILogger<ScheduledJobsSyncHostedService>>();
        _loggerMock.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        _schedulerFactoryMock
            .Setup(f => f.GetScheduler(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_schedulerMock.Object);
    }

    [Test]
    public async Task StartAsync_WhenTriggersMissing_ShouldAddJobsAndScheduleWithDefaultsAndOverrides()
    {
        var descriptors = new[]
        {
            RecurringJobDescriptor.FromType(typeof(SprintLeaderboardJob)),
            RecurringJobDescriptor.FromType(typeof(PeerAssessmentReminderJob))
        };

        var options = Options.Create(new ScheduledJobsOptions
        {
            DefaultTimeZoneId = "Europe/Brussels",
            Jobs = new Dictionary<string, ScheduledJobConfig>(StringComparer.OrdinalIgnoreCase)
            {
                ["SprintLeaderboard"] = new() { Enabled = true, CronExpression = "0 5 0 ? * MON" }
            }
        });

        _schedulerMock
            .Setup(s => s.GetTrigger(It.IsAny<TriggerKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ITrigger)null);
        _schedulerMock
            .Setup(s => s.GetTriggerState(It.IsAny<TriggerKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TriggerState.Normal);

        var scheduledTriggers = new List<ICronTrigger>();
        _schedulerMock
            .Setup(s => s.ScheduleJob(It.IsAny<ITrigger>(), It.IsAny<ScheduleJobOptions>(), It.IsAny<CancellationToken>()))
            .Callback<ITrigger, ScheduleJobOptions, CancellationToken>((t, _, _) => scheduledTriggers.Add((ICronTrigger)t))
            .ReturnsAsync(DateTimeOffset.UtcNow);

        var service = new ScheduledJobsSyncHostedService(_schedulerFactoryMock.Object, descriptors, options, _loggerMock.Object);

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        _schedulerMock.Verify(s => s.AddJob(It.IsAny<IJobDetail>(), It.Is<AddJobOptions>(o => o.Replace), It.IsAny<CancellationToken>()), Times.Exactly(2));
        Assert.That(scheduledTriggers.Count, Is.EqualTo(2));
        Assert.That(scheduledTriggers[0].CronExpressionString, Is.EqualTo("0 5 0 ? * MON"));
        Assert.That(scheduledTriggers[0].MisfireInstruction, Is.EqualTo(CronTriggerMisfireInstruction.FireAndProceed));
        Assert.That(scheduledTriggers[1].CronExpressionString, Is.EqualTo("0 0 */2 ? * *"));
        Assert.That(scheduledTriggers[1].MisfireInstruction, Is.EqualTo(CronTriggerMisfireInstruction.DoNothing));
    }

    [Test]
    public async Task StartAsync_WhenTriggerConfigurationChanged_ShouldRescheduleJob()
    {
        var descriptors = new[] { RecurringJobDescriptor.FromType(typeof(StreakReminderJob)) };
        var options = Options.Create(new ScheduledJobsOptions
        {
            DefaultTimeZoneId = "Europe/Brussels",
            Jobs = new Dictionary<string, ScheduledJobConfig>(StringComparer.OrdinalIgnoreCase)
            {
                ["StreakReminder"] = new() { Enabled = true, CronExpression = "0 30 12 ? * THU", TimeZoneId = "UTC" }
            }
        });

        var existingTriggerMock = new Mock<ICronTrigger>();
        existingTriggerMock.SetupGet(t => t.CronExpressionString).Returns("0 0 12 ? * THU");
        existingTriggerMock.SetupGet(t => t.TimeZone).Returns(TimeZoneInfo.FindSystemTimeZoneById("Europe/Brussels"));
        existingTriggerMock.SetupGet(t => t.MisfireInstruction).Returns(CronTriggerMisfireInstruction.FireAndProceed);

        _schedulerMock
            .Setup(s => s.GetTrigger(It.IsAny<TriggerKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTriggerMock.Object);
        _schedulerMock
            .Setup(s => s.GetTriggerState(It.IsAny<TriggerKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TriggerState.Paused);

        var service = new ScheduledJobsSyncHostedService(_schedulerFactoryMock.Object, descriptors, options, _loggerMock.Object);

        await service.StartAsync(CancellationToken.None);

        _schedulerMock.Verify(s => s.RescheduleJob(
            new TriggerKey("StreakReminder-trigger", ScheduledJobsSyncHostedService.DefaultGroup),
            It.Is<ITrigger>(t => ((ICronTrigger)t).CronExpressionString == "0 30 12 ? * THU"),
            It.IsAny<CancellationToken>()), Times.Once);
        _schedulerMock.Verify(s => s.ResumeJob(new JobKey("StreakReminder", ScheduledJobsSyncHostedService.DefaultGroup), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task StartAsync_WhenTriggerUnchangedAndJobDisabled_ShouldNotRescheduleAndShouldPauseJob()
    {
        var descriptors = new[] { RecurringJobDescriptor.FromType(typeof(StreakReminderJob)) };
        var options = Options.Create(new ScheduledJobsOptions
        {
            DefaultTimeZoneId = "",
            Jobs = new Dictionary<string, ScheduledJobConfig>(StringComparer.OrdinalIgnoreCase)
            {
                ["StreakReminder"] = new() { Enabled = false }
            }
        });

        var brusselsTz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Brussels");
        var existingTriggerMock = new Mock<ICronTrigger>();
        existingTriggerMock.SetupGet(t => t.CronExpressionString).Returns("0 0 12 ? * THU");
        existingTriggerMock.SetupGet(t => t.TimeZone).Returns(brusselsTz);
        existingTriggerMock.SetupGet(t => t.MisfireInstruction).Returns(CronTriggerMisfireInstruction.FireAndProceed);

        _schedulerMock
            .Setup(s => s.GetTrigger(It.IsAny<TriggerKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTriggerMock.Object);

        var service = new ScheduledJobsSyncHostedService(_schedulerFactoryMock.Object, descriptors, options, _loggerMock.Object);

        await service.StartAsync(CancellationToken.None);

        _schedulerMock.Verify(s => s.RescheduleJob(It.IsAny<TriggerKey>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Never);
        _schedulerMock.Verify(s => s.PauseJob(new JobKey("StreakReminder", ScheduledJobsSyncHostedService.DefaultGroup), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task StartAsync_WhenExistingTriggerIsNotCronTrigger_ShouldReschedule()
    {
        var descriptors = new[] { RecurringJobDescriptor.FromType(typeof(StreakReminderJob)) };
        var options = Options.Create(new ScheduledJobsOptions());

        _schedulerMock
            .Setup(s => s.GetTrigger(It.IsAny<TriggerKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<ITrigger>());
        _schedulerMock
            .Setup(s => s.GetTriggerState(It.IsAny<TriggerKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TriggerState.Normal);

        var service = new ScheduledJobsSyncHostedService(_schedulerFactoryMock.Object, descriptors, options, _loggerMock.Object);

        await service.StartAsync(CancellationToken.None);

        _schedulerMock.Verify(s => s.RescheduleJob(It.IsAny<TriggerKey>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void StartAsync_WhenDuplicateOrEmptyJobNameOrInvalidCron_ShouldThrow()
    {
        var duplicateDescriptors = new[]
        {
            new RecurringJobDescriptor(typeof(StreakReminderJob), "Dup", "Desc", "0 0 12 ? * THU", MisfirePolicy.FireAndProceed, 3, TimeSpan.FromMinutes(15)),
            new RecurringJobDescriptor(typeof(SprintLeaderboardJob), "dup", "Desc", "0 1 0 ? * MON", MisfirePolicy.FireAndProceed, 3, TimeSpan.FromHours(1))
        };
        var duplicateService = new ScheduledJobsSyncHostedService(_schedulerFactoryMock.Object, duplicateDescriptors, Options.Create(new ScheduledJobsOptions()), _loggerMock.Object);
        Assert.ThrowsAsync<InvalidOperationException>(() => duplicateService.StartAsync(CancellationToken.None));

        var emptyNameDescriptors = new[]
        {
            new RecurringJobDescriptor(typeof(StreakReminderJob), " ", "Desc", "0 0 12 ? * THU", MisfirePolicy.FireAndProceed, 3, TimeSpan.FromMinutes(15))
        };
        var emptyNameService = new ScheduledJobsSyncHostedService(_schedulerFactoryMock.Object, emptyNameDescriptors, Options.Create(new ScheduledJobsOptions()), _loggerMock.Object);
        Assert.ThrowsAsync<InvalidOperationException>(() => emptyNameService.StartAsync(CancellationToken.None));

        var invalidCronDescriptors = new[] { RecurringJobDescriptor.FromType(typeof(StreakReminderJob)) };
        var invalidCronOptions = Options.Create(new ScheduledJobsOptions
        {
            Jobs = new Dictionary<string, ScheduledJobConfig>(StringComparer.OrdinalIgnoreCase)
            {
                ["StreakReminder"] = new() { CronExpression = "not-a-cron" }
            }
        });
        var invalidCronService = new ScheduledJobsSyncHostedService(_schedulerFactoryMock.Object, invalidCronDescriptors, invalidCronOptions, _loggerMock.Object);
        Assert.ThrowsAsync<InvalidOperationException>(() => invalidCronService.StartAsync(CancellationToken.None));
    }
}
