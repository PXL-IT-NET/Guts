using System;
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
public class SprintLeaderboardBackgroundServiceTests
{
    private Mock<IServiceScopeFactory> _scopeFactoryMock;
    private Mock<IServiceScope> _scopeMock;
    private Mock<IServiceProvider> _serviceProviderMock;
    private Mock<IGamificationService> _gamificationServiceMock;
    private Mock<ILogger<SprintLeaderboardBackgroundService>> _loggerMock;
    private SprintLeaderboardBackgroundService _service;

    [SetUp]
    public void Setup()
    {
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _scopeMock = new Mock<IServiceScope>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _gamificationServiceMock = new Mock<IGamificationService>();
        _loggerMock = new Mock<ILogger<SprintLeaderboardBackgroundService>>();
        _loggerMock.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(_scopeMock.Object);
        _scopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IGamificationService))).Returns(_gamificationServiceMock.Object);

        _service = new SprintLeaderboardBackgroundService(_scopeFactoryMock.Object, _loggerMock.Object);
    }

    [Test]
    public void GetNextMondayMorningTrigger_FromSunday_ShouldReturnUpcomingMondayAt0001()
    {
        var sunday = new DateTime(2026, 10, 25, 20, 0, 0, DateTimeKind.Utc);
        var result = SprintLeaderboardBackgroundService.GetNextMondayMorningTrigger(sunday);

        Assert.That(result, Is.EqualTo(new DateTime(2026, 10, 26, 0, 1, 0, DateTimeKind.Utc)));
        Assert.That(result.DayOfWeek, Is.EqualTo(DayOfWeek.Monday));
        Assert.That(result.Hour, Is.EqualTo(0));
        Assert.That(result.Minute, Is.EqualTo(1));
        Assert.That(result.Second, Is.EqualTo(0));
    }

    [Test]
    public void GetNextMondayMorningTrigger_FromMondayBefore0001_ShouldReturnSameMondayAt0001()
    {
        var mondayEarly = new DateTime(2026, 10, 26, 0, 0, 30, DateTimeKind.Utc);
        var result = SprintLeaderboardBackgroundService.GetNextMondayMorningTrigger(mondayEarly);

        Assert.That(result, Is.EqualTo(new DateTime(2026, 10, 26, 0, 1, 0, DateTimeKind.Utc)));
    }

    [Test]
    public void GetNextMondayMorningTrigger_FromMondayAfter0001_ShouldReturnNextMondayAt0001()
    {
        var mondayAfter = new DateTime(2026, 10, 26, 0, 1, 1, DateTimeKind.Utc);
        var result = SprintLeaderboardBackgroundService.GetNextMondayMorningTrigger(mondayAfter);

        Assert.That(result, Is.EqualTo(new DateTime(2026, 11, 2, 0, 1, 0, DateTimeKind.Utc)));
    }

    [Test]
    public void GetPreviousCompletedWeek_FromAnyDayOfWeek_ShouldReturnPrecedingCompletedIsoWeek()
    {
        // Monday Nov 2, 2026 (Week 45) -> Previous completed week is Week 44 of 2026
        var monday = new DateTime(2026, 11, 2, 9, 0, 0, DateTimeKind.Utc);
        var (year1, week1) = SprintLeaderboardBackgroundService.GetPreviousCompletedWeek(monday);
        Assert.That(year1, Is.EqualTo(2026));
        Assert.That(week1, Is.EqualTo(44));

        // Wednesday Nov 4, 2026 (Week 45) -> Still Week 44
        var wednesday = new DateTime(2026, 11, 4, 15, 0, 0, DateTimeKind.Utc);
        var (year2, week2) = SprintLeaderboardBackgroundService.GetPreviousCompletedWeek(wednesday);
        Assert.That(year2, Is.EqualTo(2026));
        Assert.That(week2, Is.EqualTo(44));

        // Sunday Nov 8, 2026 (Week 45) -> Still Week 44
        var sunday = new DateTime(2026, 11, 8, 23, 59, 0, DateTimeKind.Utc);
        var (year3, week3) = SprintLeaderboardBackgroundService.GetPreviousCompletedWeek(sunday);
        Assert.That(year3, Is.EqualTo(2026));
        Assert.That(week3, Is.EqualTo(44));

        // Monday Nov 9, 2026 (Week 46) -> Week 45
        var nextMonday = new DateTime(2026, 11, 9, 0, 1, 0, DateTimeKind.Utc);
        var (year4, week4) = SprintLeaderboardBackgroundService.GetPreviousCompletedWeek(nextMonday);
        Assert.That(year4, Is.EqualTo(2026));
        Assert.That(week4, Is.EqualTo(45));
    }

    [Test]
    public async Task ExecuteSprintAwardingAsync_ShouldCallGamificationServiceWithPreviousWeekYearAndWeekNumber()
    {
        // Monday Oct 26, 2026 is week 44; previous week was week 43 of 2026
        var executionTime = new DateTime(2026, 10, 26, 0, 1, 0, DateTimeKind.Utc);

        await _service.ExecuteSprintAwardingAsync(executionTime, CancellationToken.None);

        _scopeFactoryMock.Verify(f => f.CreateScope(), Times.Once);
        _gamificationServiceMock.Verify(s => s.AwardWeeklySprintBadgesAsync(2026, 43, null), Times.Once);
        Assert.That(_service.LastSuccessfulWeek, Is.EqualTo((2026, 43)));
        Assert.That(_service.LastRunFailed, Is.False);
    }

    [Test]
    public async Task ExecuteSprintAwardingAsync_WhenLoggerDisabled_ShouldStillCallGamificationService()
    {
        _loggerMock.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(false);
        var executionTime = new DateTime(2026, 10, 26, 0, 1, 0, DateTimeKind.Utc);

        await _service.ExecuteSprintAwardingAsync(executionTime, CancellationToken.None);

        _gamificationServiceMock.Verify(s => s.AwardWeeklySprintBadgesAsync(2026, 43, null), Times.Once);
    }

    [Test]
    public async Task RunAwardingSafelyAsync_WhenAlreadyCompletedForSameWeek_ShouldSkipExecution()
    {
        var executionTime = new DateTime(2026, 11, 2, 9, 0, 0, DateTimeKind.Utc);

        bool firstRunResult = await _service.RunAwardingSafelyAsync(executionTime, CancellationToken.None);
        Assert.That(firstRunResult, Is.True);
        _gamificationServiceMock.Verify(s => s.AwardWeeklySprintBadgesAsync(2026, 44, null), Times.Once);

        bool secondRunResult = await _service.RunAwardingSafelyAsync(executionTime, CancellationToken.None);
        Assert.That(secondRunResult, Is.True);
        // Should not call service again for the same completed week
        _gamificationServiceMock.Verify(s => s.AwardWeeklySprintBadgesAsync(2026, 44, null), Times.Once);
    }

    [Test]
    public async Task RunAwardingSafelyAsync_WhenAwardingThrows_ShouldSetLastRunFailedAndReturnFalse()
    {
        var executionTime = new DateTime(2026, 11, 2, 9, 0, 0, DateTimeKind.Utc);
        _gamificationServiceMock.Setup(s => s.AwardWeeklySprintBadgesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>()))
            .ThrowsAsync(new InvalidOperationException("DB connection error"));

        bool result = await _service.RunAwardingSafelyAsync(executionTime, CancellationToken.None);

        Assert.That(result, Is.False);
        Assert.That(_service.LastRunFailed, Is.True);
        Assert.That(_service.LastSuccessfulWeek, Is.Null);
    }

    [Test]
    public async Task StartAsync_WhenStoppingTokenCancelled_ShouldStopWithoutThrowing()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await _service.StartAsync(cts.Token);
        await _service.StopAsync(CancellationToken.None);

        Assert.Pass();
    }

    [Test]
    public async Task StartAsync_WhenRunning_ShouldRunOnStartupAndThenCancelDelayGracefully()
    {
        using var cts = new CancellationTokenSource();

        await _service.StartAsync(cts.Token);
        await Task.Delay(50);
        await _service.StopAsync(CancellationToken.None);

        // Should have run on startup
        _gamificationServiceMock.Verify(s => s.AwardWeeklySprintBadgesAsync(It.IsAny<int>(), It.IsAny<int>(), null), Times.Once);
        Assert.That(_service.LastSuccessfulWeek, Is.Not.Null);
        Assert.That(_service.LastRunFailed, Is.False);
    }

    [Test]
    public async Task ExecuteAsync_WhenStartupAwardingFails_ShouldRetryAfterRetryInterval()
    {
        _service.RetryInterval = TimeSpan.FromMilliseconds(50);

        int callCount = 0;
        _gamificationServiceMock.Setup(s => s.AwardWeeklySprintBadgesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new InvalidOperationException("Transient startup failure");
                }
                return Task.CompletedTask;
            });

        using var cts = new CancellationTokenSource();

        await _service.StartAsync(cts.Token);

        // Wait enough time for the retry to fire
        await Task.Delay(150);

        await _service.StopAsync(CancellationToken.None);

        Assert.That(callCount, Is.GreaterThanOrEqualTo(2));
        Assert.That(_service.LastRunFailed, Is.False);
        Assert.That(_service.LastSuccessfulWeek, Is.Not.Null);
    }
}
