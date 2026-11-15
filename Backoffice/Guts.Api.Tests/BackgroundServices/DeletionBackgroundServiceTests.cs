using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Guts.Api.BackgroundServices;
using Guts.Application.Services.Deletion;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Guts.Api.Tests.BackgroundServices;

[TestFixture]
public class DeletionBackgroundServiceTests
{
    private Mock<IServiceScopeFactory> _scopeFactoryMock;
    private Mock<IServiceScope> _scopeMock;
    private Mock<IServiceProvider> _serviceProviderMock;
    private Mock<IDeletionJobService> _deletionJobServiceMock;
    private Mock<ILogger<DeletionBackgroundService>> _loggerMock;
    private InMemoryDeletionJobQueue _queue;

    [SetUp]
    public void Setup()
    {
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _scopeMock = new Mock<IServiceScope>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _deletionJobServiceMock = new Mock<IDeletionJobService>();
        _loggerMock = new Mock<ILogger<DeletionBackgroundService>>();
        _loggerMock.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        _queue = new InMemoryDeletionJobQueue();

        _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(_scopeMock.Object);
        _scopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IDeletionJobService))).Returns(_deletionJobServiceMock.Object);
    }

    [Test]
    public async Task ExecuteAsync_WhenDisabledInConfiguration_ShouldNotProcessJobs()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string> { { "Deletion:Enabled", "false" } })
            .Build();

        using var service = new DeletionBackgroundService(_scopeFactoryMock.Object, _queue, config, _loggerMock.Object);
        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        _deletionJobServiceMock.Verify(
            s => s.ProcessPendingJobsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ExecuteAsync_ProcessesOnStartupAndWhenQueuedWithConfiguredBatchSize()
    {
        var startupCalled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var queuedCalled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int callCount = 0;

        _deletionJobServiceMock
            .Setup(s => s.ProcessPendingJobsAsync(250, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                int current = Interlocked.Increment(ref callCount);
                if (current == 1) startupCalled.TrySetResult();
                else if (current >= 2) queuedCalled.TrySetResult();
                return 1;
            });

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string> { { "Deletion:BatchSize", "250" } })
            .Build();

        using var service = new DeletionBackgroundService(_scopeFactoryMock.Object, _queue, config, _loggerMock.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await service.StartAsync(cts.Token);
        await startupCalled.Task;

        await _queue.EnqueueAsync(Guid.NewGuid(), cts.Token);
        await queuedCalled.Task;

        await service.StopAsync(CancellationToken.None);

        _deletionJobServiceMock.Verify(s => s.ProcessPendingJobsAsync(250, It.IsAny<CancellationToken>()), Times.AtLeast(2));
    }

    [Test]
    public async Task ExecuteAsync_WhenProcessingThrows_LogsErrorAndContinuesRunning()
    {
        var secondCallSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int callCount = 0;

        _deletionJobServiceMock
            .Setup(s => s.ProcessPendingJobsAsync(IDeletionJobService.DefaultBatchSize, It.IsAny<CancellationToken>()))
            .Returns<int, CancellationToken>((_, _) =>
            {
                int current = Interlocked.Increment(ref callCount);
                if (current == 1)
                {
                    throw new InvalidOperationException("Transient failure");
                }
                secondCallSignal.TrySetResult();
                return Task.FromResult(0);
            });

        using var service = new DeletionBackgroundService(
            _scopeFactoryMock.Object,
            _queue,
            new ConfigurationBuilder().Build(),
            _loggerMock.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await service.StartAsync(cts.Token);
        await _queue.EnqueueAsync(Guid.NewGuid(), cts.Token);
        await secondCallSignal.Task;

        await service.StopAsync(CancellationToken.None);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
    }
}
