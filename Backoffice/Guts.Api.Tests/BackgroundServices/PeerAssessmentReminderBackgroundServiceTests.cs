using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Guts.Api.BackgroundServices;
using Guts.Application.Services.Assessment;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Guts.Api.Tests.BackgroundServices;

[TestFixture]
public class PeerAssessmentReminderBackgroundServiceTests
{
    private Mock<IServiceScopeFactory> _scopeFactoryMock;
    private Mock<IServiceScope> _scopeMock;
    private Mock<IServiceProvider> _serviceProviderMock;
    private Mock<IPeerAssessmentReminderService> _reminderServiceMock;
    private Mock<ILogger<PeerAssessmentReminderBackgroundService>> _loggerMock;
    private ControllableTimeProvider _timeProvider;

    [SetUp]
    public void Setup()
    {
        _scopeFactoryMock = new();
        _scopeMock = new();
        _serviceProviderMock = new();
        _reminderServiceMock = new();
        _loggerMock = new();
        _loggerMock.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(_scopeMock.Object);
        _scopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IPeerAssessmentReminderService))).Returns(_reminderServiceMock.Object);

        _timeProvider = new(new(2026, 11, 11, 10, 0, 0, DateTimeKind.Utc));
    }

    [Test]
    public async Task ExecuteAsync_DefaultInterval_ShouldScheduleForTwoHoursAndDispatchReminders()
    {
        var config = new ConfigurationBuilder().Build();
        using var service = new PeerAssessmentReminderBackgroundService(
            _scopeFactoryMock.Object,
            config,
            _loggerMock.Object,
            _timeProvider);

        _timeProvider.MaxAutoFires = 1;

        await service.StartAsync(CancellationToken.None);
        await _timeProvider.WaitForTimerCreationCountAsync(2);
        await service.StopAsync(CancellationToken.None);

        Assert.That(_timeProvider.ScheduledDelays[0], Is.EqualTo(TimeSpan.FromHours(2)));
        _scopeFactoryMock.Verify(f => f.CreateScope(), Times.Once);
        _reminderServiceMock.Verify(
            s => s.SendAssessmentRemindersAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task ExecuteAsync_ConfiguredInterval_ShouldUseConfiguredInterval()
    {
        var inMemoryConfig = new Dictionary<string, string>
        {
            { "PeerAssessmentReminders:CheckIntervalInHours", "4" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();

        using var service = new PeerAssessmentReminderBackgroundService(
            _scopeFactoryMock.Object,
            config,
            _loggerMock.Object,
            _timeProvider);

        _timeProvider.MaxAutoFires = 1;

        await service.StartAsync(CancellationToken.None);
        await _timeProvider.WaitForTimerCreationCountAsync(2);
        await service.StopAsync(CancellationToken.None);

        Assert.That(_timeProvider.ScheduledDelays[0], Is.EqualTo(TimeSpan.FromHours(4)));
        _reminderServiceMock.Verify(
            s => s.SendAssessmentRemindersAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task ExecuteAsync_WhenReminderServiceThrows_ShouldCatchLogErrorAndContinue()
    {
        var config = new ConfigurationBuilder().Build();
        _reminderServiceMock
            .Setup(s => s.SendAssessmentRemindersAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB connection error"));

        using var service = new PeerAssessmentReminderBackgroundService(
            _scopeFactoryMock.Object,
            config,
            _loggerMock.Object,
            _timeProvider);

        _timeProvider.MaxAutoFires = 1;

        await service.StartAsync(CancellationToken.None);
        await _timeProvider.WaitForTimerCreationCountAsync(2);
        await service.StopAsync(CancellationToken.None);

        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    private sealed class ControllableTimeProvider(DateTime initialUtcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = new(DateTime.SpecifyKind(initialUtcNow, DateTimeKind.Utc));
        private int _createdTimersCount;
        private readonly List<TaskCompletionSource> _waiters = [];

        public List<TimeSpan> ScheduledDelays { get; } = [];
        public int MaxAutoFires { get; set; }

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
