using System;
using System.Threading;
using System.Threading.Tasks;
using Guts.Api.BackgroundServices;
using Guts.Business.Services;
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
    public async Task ExecuteSprintAwardingAsync_ShouldCallGamificationServiceWithPreviousWeekYearAndWeekNumber()
    {
        // Monday Oct 26, 2026 is week 44; previous week was week 43 of 2026
        var executionTime = new DateTime(2026, 10, 26, 0, 1, 0, DateTimeKind.Utc);

        await _service.ExecuteSprintAwardingAsync(executionTime, CancellationToken.None);

        _scopeFactoryMock.Verify(f => f.CreateScope(), Times.Once);
        _gamificationServiceMock.Verify(s => s.AwardWeeklySprintBadgesAsync(2026, 43, null), Times.Once);
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
    public async Task StartAsync_WhenStoppingTokenCancelled_ShouldStopWithoutThrowing()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await _service.StartAsync(cts.Token);
        await _service.StopAsync(CancellationToken.None);

        Assert.Pass();
    }

    [Test]
    public async Task StartAsync_WhenRunningAndThenStopped_ShouldEnterLoopAndCancelDelayGracefully()
    {
        using var cts = new CancellationTokenSource();

        await _service.StartAsync(cts.Token);
        await Task.Delay(50);
        await _service.StopAsync(CancellationToken.None);

        Assert.Pass();
    }
}