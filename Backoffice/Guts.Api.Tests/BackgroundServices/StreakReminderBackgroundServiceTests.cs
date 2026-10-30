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
public class StreakReminderBackgroundServiceTests
{
    private Mock<IServiceScopeFactory> _scopeFactoryMock;
    private Mock<IServiceScope> _scopeMock;
    private Mock<IServiceProvider> _serviceProviderMock;
    private Mock<IStreakReminderService> _streakReminderServiceMock;
    private Mock<ILogger<StreakReminderBackgroundService>> _loggerMock;
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

        _service = new StreakReminderBackgroundService(_scopeFactoryMock.Object, _loggerMock.Object);
    }

    [Test]
    public void GetNextThursdayMiddayTrigger_FromWednesday_ShouldReturnUpcomingThursdayAt1200()
    {
        var wednesday = new DateTime(2026, 10, 28, 15, 30, 0, DateTimeKind.Utc);
        var result = StreakReminderBackgroundService.GetNextThursdayMiddayTrigger(wednesday);

        Assert.That(result, Is.EqualTo(new DateTime(2026, 10, 29, 12, 0, 0, DateTimeKind.Utc)));
        Assert.That(result.DayOfWeek, Is.EqualTo(DayOfWeek.Thursday));
        Assert.That(result.Hour, Is.EqualTo(12));
        Assert.That(result.Minute, Is.EqualTo(0));
        Assert.That(result.Second, Is.EqualTo(0));
    }

    [Test]
    public void GetNextThursdayMiddayTrigger_FromThursdayBefore1200_ShouldReturnSameThursdayAt1200()
    {
        var thursdayEarly = new DateTime(2026, 10, 29, 11, 45, 0, DateTimeKind.Utc);
        var result = StreakReminderBackgroundService.GetNextThursdayMiddayTrigger(thursdayEarly);

        Assert.That(result, Is.EqualTo(new DateTime(2026, 10, 29, 12, 0, 0, DateTimeKind.Utc)));
    }

    [Test]
    public void GetNextThursdayMiddayTrigger_FromThursdayAfter1200_ShouldReturnNextThursdayAt1200()
    {
        var thursdayAfter = new DateTime(2026, 10, 29, 12, 0, 1, DateTimeKind.Utc);
        var result = StreakReminderBackgroundService.GetNextThursdayMiddayTrigger(thursdayAfter);

        Assert.That(result, Is.EqualTo(new DateTime(2026, 11, 5, 12, 0, 0, DateTimeKind.Utc)));
    }

    [Test]
    public async Task ExecuteStreakRemindersAsync_ShouldCallStreakReminderServiceWithExecutionTime()
    {
        var executionTime = new DateTime(2026, 10, 29, 12, 0, 0, DateTimeKind.Utc);

        await _service.ExecuteStreakRemindersAsync(executionTime, CancellationToken.None);

        _scopeFactoryMock.Verify(f => f.CreateScope(), Times.Once);
        _streakReminderServiceMock.Verify(s => s.SendStreakRemindersAsync(executionTime, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task StartAsync_WhenStoppingTokenCancelled_ShouldStopWithoutThrowing()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await _service.StartAsync(cts.Token);
        await _service.StopAsync(CancellationToken.None);
    }
}

