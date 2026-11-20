using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Guts.Api.ScheduledJobs;
using Guts.Api.ScheduledJobs.Jobs;
using Guts.Application.Dtos;
using Guts.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Quartz;

namespace Guts.Api.Tests.ScheduledJobs;

[TestFixture]
public class TestRunPruningJobTests
{
    [Test]
    public void RecurringJobDescriptor_FromType_ShouldReadTestRunPruningMetadata()
    {
        var descriptor = RecurringJobDescriptor.FromType(typeof(TestRunPruningJob));

        Assert.That(descriptor.JobName, Is.EqualTo("TestRunPruning"));
        Assert.That(descriptor.DefaultCronExpression, Is.EqualTo("0 0 3 ? * SUN"));
        Assert.That(descriptor.MisfirePolicy, Is.EqualTo(MisfirePolicy.FireAndProceed));
        Assert.That(descriptor.MaxRetries, Is.EqualTo(3));
        Assert.That(descriptor.RetryDelay, Is.EqualTo(TimeSpan.FromMinutes(15)));
        Assert.That(descriptor.Description, Is.Not.Empty);
    }

    [Test]
    public async Task Execute_WhenBatchSizeNotConfigured_ShouldUseDefaultBatchSize()
    {
        var serviceMock = new Mock<ITestRunPruningService>();
        var loggerMock = new Mock<ILogger<TestRunPruningJob>>();
        loggerMock.Setup(l => l.IsEnabled(LogLevel.Information)).Returns(true);

        var config = new ConfigurationBuilder().Build();
        using var cts = new CancellationTokenSource();

        serviceMock
            .Setup(s => s.PruneRedundantTestRunsAsync(ITestRunPruningService.DefaultBatchSize, cts.Token))
            .ReturnsAsync(new TestRunPruningResult(3, 10, 40));

        var job = new TestRunPruningJob(serviceMock.Object, config, loggerMock.Object);

        await job.Execute(Mock.Of<IJobExecutionContext>(), cts.Token);

        serviceMock.Verify(
            s => s.PruneRedundantTestRunsAsync(ITestRunPruningService.DefaultBatchSize, cts.Token),
            Times.Once);
    }

    [Test]
    public async Task Execute_WhenBatchSizeConfigured_ShouldPassConfiguredBatchSize()
    {
        var serviceMock = new Mock<ITestRunPruningService>();
        var loggerMock = new Mock<ILogger<TestRunPruningJob>>();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["TestRunPruning:BatchSize"] = "250"
            })
            .Build();

        serviceMock
            .Setup(s => s.PruneRedundantTestRunsAsync(250, CancellationToken.None))
            .ReturnsAsync(new TestRunPruningResult(1, 2, 6));

        var job = new TestRunPruningJob(serviceMock.Object, config, loggerMock.Object);

        await job.Execute(Mock.Of<IJobExecutionContext>(), CancellationToken.None);

        serviceMock.Verify(s => s.PruneRedundantTestRunsAsync(250, CancellationToken.None), Times.Once);
    }
}
