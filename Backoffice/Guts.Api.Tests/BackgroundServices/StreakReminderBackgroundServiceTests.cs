using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Guts.Api.BackgroundServices;
using Guts.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Guts.Api.Tests.BackgroundServices;

[TestFixture]
public class StreakReminderBackgroundServiceTests
{
    private Mock<IServiceScopeFactory> _scopeFactoryMock;
    private Mock<IServiceScope> _scopeMock;
    private Mock<IServiceProvider> _serviceProviderMock;
    private Mock<IStreakReminderService> _streakReminderServiceMock;
    private Mock<ILogger<StreakReminderBackgroundService>> _loggerMock;
    private ControllableTimeProvider _timeProvider;
    private StreakReminderBackgroundService _service;

    [SetUp]
    public void Setup()
    {
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _scopeMock = new Mock<IServiceScope>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _streakReminderServiceMock = new Mock<IStreakReminderService>();
        _loggerMock = new Mock<ILogger<StreakReminderBackgroundService>>();
        _loggerMock.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(_scopeMock.Object);
        _scopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IStreakReminderService))).Returns(_streakReminderServiceMock.Object);

        _timeProvider = new ControllableTimeProvider(new DateTime(2026, 10, 28, 15, 30, 0, DateTimeKind.Utc));
        _service = new StreakReminderBackgroundService(_scopeFactoryMock.Object, _loggerMock.Object, _timeProvider);
    }

    [TearDown]
    public void TearDown()
    {
        _service?.Dispose();
    }

    [Test]
    public async Task ExecuteAsync_FromWednesday_ShouldScheduleForUpcomingThursdayAt1200AndSendReminders()
    {
        var wednesday = new DateTime(2026, 10, 28, 15, 30, 0, DateTimeKind.Utc);
        var expectedThursdayTrigger = new DateTime(2026, 10, 29, 12, 0, 0, DateTimeKind.Utc);
        _timeProvider.SetUtcNow(wednesday);
        _timeProvider.MaxAutoFires = 1;

        await _service.StartAsync(CancellationToken.None);
        await _timeProvider.WaitForTimerCreationCountAsync(2);
        await _service.StopAsync(CancellationToken.None);

        Assert.That(_timeProvider.ScheduledDelays[0], Is.EqualTo(expectedThursdayTrigger - wednesday));
        _scopeFactoryMock.Verify(f => f.CreateScope(), Times.Once);
        _streakReminderServiceMock.Verify(
            s => s.SendStreakRemindersAsync(expectedThursdayTrigger, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task ExecuteAsync_FromThursdayBefore1200_ShouldScheduleForSameThursdayAt1200AndSendReminders()
    {
        var thursdayEarly = new DateTime(2026, 10, 29, 11, 45, 0, DateTimeKind.Utc);
        var expectedThursdayTrigger = new DateTime(2026, 10, 29, 12, 0, 0, DateTimeKind.Utc);
        _timeProvider.SetUtcNow(thursdayEarly);
        _timeProvider.MaxAutoFires = 1;

        await _service.StartAsync(CancellationToken.None);
        await _timeProvider.WaitForTimerCreationCountAsync(2);
        await _service.StopAsync(CancellationToken.None);

        Assert.That(_timeProvider.ScheduledDelays[0], Is.EqualTo(expectedThursdayTrigger - thursdayEarly));
        _streakReminderServiceMock.Verify(
            s => s.SendStreakRemindersAsync(expectedThursdayTrigger, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task ExecuteAsync_FromThursdayAfter1200_ShouldScheduleForNextThursdayAt1200AndSendReminders()
    {
        var thursdayAfter = new DateTime(2026, 10, 29, 12, 0, 1, DateTimeKind.Utc);
        var expectedNextThursdayTrigger = new DateTime(2026, 11, 5, 12, 0, 0, DateTimeKind.Utc);
        _timeProvider.SetUtcNow(thursdayAfter);
        _timeProvider.MaxAutoFires = 1;

        await _service.StartAsync(CancellationToken.None);
        await _timeProvider.WaitForTimerCreationCountAsync(2);
        await _service.StopAsync(CancellationToken.None);

        Assert.That(_timeProvider.ScheduledDelays[0], Is.EqualTo(expectedNextThursdayTrigger - thursdayAfter));
        _streakReminderServiceMock.Verify(
            s => s.SendStreakRemindersAsync(expectedNextThursdayTrigger, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task ExecuteAsync_WhenInformationLoggingDisabled_ShouldStillSendStreakReminders()
    {
        _loggerMock.Setup(l => l.IsEnabled(LogLevel.Information)).Returns(false);
        var wednesday = new DateTime(2026, 10, 28, 15, 30, 0, DateTimeKind.Utc);
        var expectedThursdayTrigger = new DateTime(2026, 10, 29, 12, 0, 0, DateTimeKind.Utc);
        _timeProvider.SetUtcNow(wednesday);
        _timeProvider.MaxAutoFires = 1;

        await _service.StartAsync(CancellationToken.None);
        await _timeProvider.WaitForTimerCreationCountAsync(2);
        await _service.StopAsync(CancellationToken.None);

        _streakReminderServiceMock.Verify(
            s => s.SendStreakRemindersAsync(expectedThursdayTrigger, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task ExecuteAsync_WhenReminderServiceThrows_ShouldLogErrorAndContinueSchedulingNextRun()
    {
        var wednesday = new DateTime(2026, 10, 28, 15, 30, 0, DateTimeKind.Utc);
        _timeProvider.SetUtcNow(wednesday);
        _timeProvider.MaxAutoFires = 2;

        int callCount = 0;
        _streakReminderServiceMock
            .Setup(s => s.SendStreakRemindersAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns((DateTime _, CancellationToken _) =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new InvalidOperationException("SMTP server unavailable");
                }
                return Task.FromResult(3);
            });

        await _service.StartAsync(CancellationToken.None);
        await _timeProvider.WaitForTimerCreationCountAsync(3);
        await _service.StopAsync(CancellationToken.None);

        Assert.That(callCount, Is.EqualTo(2));
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.Is<Exception>(ex => ex.Message == "SMTP server unavailable"),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public async Task StartAsync_WhenDefaultTimeProviderUsedAndStoppedDuringDelay_ShouldStopWithoutThrowing()
    {
        using var defaultService = new StreakReminderBackgroundService(_scopeFactoryMock.Object, _loggerMock.Object);

        await defaultService.StartAsync(CancellationToken.None);
        await defaultService.StopAsync(CancellationToken.None);

        _streakReminderServiceMock.Verify(
            s => s.SendStreakRemindersAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task StartAsync_WhenStoppingTokenAlreadyCancelled_ShouldStopImmediatelyWithoutScheduling()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await _service.StartAsync(cts.Token);
        await _service.StopAsync(CancellationToken.None);

        Assert.That(_timeProvider.ScheduledDelays, Is.Empty);
    }

    private sealed class ControllableTimeProvider(DateTime initialUtcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = new(DateTime.SpecifyKind(initialUtcNow, DateTimeKind.Utc));
        private int _createdTimersCount;
        private readonly List<TaskCompletionSource> _waiters = [];

        public List<TimeSpan> ScheduledDelays { get; } = [];
        public int MaxAutoFires { get; set; }

        public void SetUtcNow(DateTime utcNow)
        {
            _utcNow = new DateTimeOffset(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc));
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public override ITimer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
        {
            ScheduledDelays.Add(dueTime);
            int timerIndex = ++_createdTimersCount;

            if (timerIndex <= MaxAutoFires)
            {
                _utcNow = _utcNow.Add(dueTime);
                callback(state);
            }

            lock (_waiters)
            {
                if (_createdTimersCount >= _targetCount)
                {
                    foreach (var waiter in _waiters)
                    {
                        waiter.TrySetResult();
                    }
                }
            }

            return new NoOpTimer();
        }

        private int _targetCount;

        public Task WaitForTimerCreationCountAsync(int count)
        {
            lock (_waiters)
            {
                _targetCount = count;
                if (_createdTimersCount >= count)
                {
                    return Task.CompletedTask;
                }

                var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                _waiters.Add(tcs);
                return tcs.Task;
            }
        }

        private sealed class NoOpTimer : ITimer
        {
            public bool Change(TimeSpan dueTime, TimeSpan period) => true;
            public void Dispose() { }
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
