using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Guts.Application.Communication;
using NUnit.Framework;

namespace Guts.Application.Tests.Communication;

[TestFixture]
public class MailThrottlerTests
{
    [Test]
    public async Task ThrottleAsync_WhenThrottleDelayIsZeroOrNegative_ShouldCompleteImmediately()
    {
        // Arrange
        var throttler = new MailThrottler(TimeSpan.Zero);
        var stopwatch = Stopwatch.StartNew();

        // Act
        await throttler.ThrottleAsync();
        stopwatch.Stop();

        // Assert
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(100));

        // Negative delay
        throttler.ThrottleDelay = TimeSpan.FromSeconds(-1);
        stopwatch.Restart();
        await throttler.ThrottleAsync();
        stopwatch.Stop();
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(100));
    }

    [Test]
    public async Task ThrottleAsync_WhenThrottleDelayIsPositive_ShouldDelay()
    {
        // Arrange
        var delay = TimeSpan.FromMilliseconds(50);
        var throttler = new MailThrottler(delay);
        var stopwatch = Stopwatch.StartNew();

        // Act
        await throttler.ThrottleAsync();
        stopwatch.Stop();

        // Assert
        Assert.That(stopwatch.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(35));
    }

    [Test]
    public void ThrottleAsync_WhenCancellationRequested_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var throttler = new MailThrottler(TimeSpan.FromSeconds(10));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        Assert.ThrowsAsync<TaskCanceledException>(async () => await throttler.ThrottleAsync(cts.Token));
    }
}

