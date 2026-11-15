using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Guts.Application.Converters;
using Guts.Application.Dtos;
using Guts.Application.Repositories;
using Guts.Application.Services.Deletion;
using Guts.Domain.DeletionJobAggregate;
using Guts.Domain.Tests.Builders;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Guts.Application.Tests.Services.Deletion;

[TestFixture]
public class DeletionJobServiceTests
{
    private Mock<IDeletionJobRepository> _jobRepositoryMock;
    private Mock<IChunkedDeletionRepository> _chunkedDeletionRepositoryMock;
    private Mock<IDeletionJobFactory> _jobFactoryMock;
    private Mock<IDeletionJobQueue> _jobQueueMock;
    private Mock<IDeletionJobConverter> _converterMock;
    private Mock<ILogger<DeletionJobService>> _loggerMock;
    private DeletionJobService _service;

    [SetUp]
    public void SetUp()
    {
        _jobRepositoryMock = new Mock<IDeletionJobRepository>();
        _chunkedDeletionRepositoryMock = new Mock<IChunkedDeletionRepository>();
        _jobFactoryMock = new Mock<IDeletionJobFactory>();
        _jobQueueMock = new Mock<IDeletionJobQueue>();
        _converterMock = new Mock<IDeletionJobConverter>();
        _loggerMock = new Mock<ILogger<DeletionJobService>>();

        _service = new DeletionJobService(
            _jobRepositoryMock.Object,
            _chunkedDeletionRepositoryMock.Object,
            _jobFactoryMock.Object,
            _jobQueueMock.Object,
            _converterMock.Object,
            _loggerMock.Object);
    }

    [Test]
    public async Task GetJobStatusAsync_ReturnsConvertedDto()
    {
        var job = new DeletionJobBuilder().Build();
        var dto = new DeletionJobDto { JobId = job.Id };
        _jobRepositoryMock.Setup(r => r.GetByIdAsync(job.Id)).ReturnsAsync(job);
        _converterMock.Setup(c => c.ToDto(job)).Returns(dto);

        var result = await _service.GetJobStatusAsync(job.Id);

        Assert.That(result, Is.SameAs(dto));
    }

    [Test]
    public async Task GetActiveJobsAsync_FiltersByTargetTypeAndMapsDtos()
    {
        var job = new DeletionJobBuilder().Build();
        var dto = new DeletionJobDto { JobId = job.Id };
        _jobRepositoryMock.Setup(r => r.GetActiveAsync(DeletionTargetType.Course)).ReturnsAsync([job]);
        _converterMock.Setup(c => c.ToDto(job)).Returns(dto);

        var result = await _service.GetActiveJobsAsync(DeletionTargetType.Course);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.SameAs(dto));
    }

    [Test]
    public async Task QueueDeletionJobAsync_CreatesPersistsEnqueuesAndReturnsDto()
    {
        var job = new DeletionJobBuilder().Build();
        var dto = new DeletionJobDto { JobId = job.Id };
        bool persistInvoked = false;

        _jobFactoryMock
            .Setup(f => f.CreateNew(DeletionTargetType.Course, 5, "CS101 - Intro", It.IsAny<DateTime>()))
            .Returns(job);
        _converterMock.Setup(c => c.ToDto(job)).Returns(dto);

        var result = await _service.QueueDeletionJobAsync(
            DeletionTargetType.Course,
            5,
            "CS101 - Intro",
            j =>
            {
                persistInvoked = ReferenceEquals(j, job);
                return Task.CompletedTask;
            });

        Assert.That(persistInvoked, Is.True);
        Assert.That(result, Is.SameAs(dto));
        _jobQueueMock.Verify(q => q.EnqueueAsync(job.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ProcessPendingJobsAsync_PendingJob_ExecutesBatchesUpdatesProgressAndCompletes()
    {
        var job = new DeletionJobBuilder().WithTarget(DeletionTargetType.Course, 7, "CS101").Build();
        _jobRepositoryMock.Setup(r => r.GetActiveAsync(null)).ReturnsAsync([job]);
        _chunkedDeletionRepositoryMock
            .Setup(r => r.CountDependentAndRootRowsAsync(DeletionTargetType.Course, 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(25);

        _chunkedDeletionRepositoryMock
            .Setup(r => r.ExecuteChunkedDeletionAsync(
                DeletionTargetType.Course,
                7,
                10,
                It.IsAny<Func<int, string, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns<DeletionTargetType, int, int, Func<int, string, Task>, CancellationToken>(
                async (_, _, _, onBatch, _) =>
                {
                    await onBatch(10, "Deleting test results");
                    await onBatch(15, "Removing course");
                });

        int processed = await _service.ProcessPendingJobsAsync(10);

        Assert.That(processed, Is.EqualTo(1));
        Assert.That(job.Status, Is.EqualTo(DeletionJobStatus.Completed));
        Assert.That(job.DeletedRows, Is.EqualTo(25));
        Assert.That(job.TotalRows, Is.EqualTo(25));
        _jobRepositoryMock.Verify(r => r.UpdateAsync(job), Times.Exactly(4));
    }

    [Test]
    public async Task ProcessPendingJobsAsync_InProgressJobAndNonPositiveBatchSize_ResumesWithDefaultBatchSize()
    {
        var job = new DeletionJobBuilder().Build();
        job.Start(100, DateTime.UtcNow.AddMinutes(-1), "Interrupted phase");
        job.RecordBatchProgress(40, "Interrupted phase");

        _jobRepositoryMock.Setup(r => r.GetActiveAsync(null)).ReturnsAsync([job]);
        _chunkedDeletionRepositoryMock
            .Setup(r => r.CountDependentAndRootRowsAsync(job.TargetType, job.TargetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(60);

        int observedBatchSize = 0;
        _chunkedDeletionRepositoryMock
            .Setup(r => r.ExecuteChunkedDeletionAsync(
                job.TargetType,
                job.TargetId,
                It.IsAny<int>(),
                It.IsAny<Func<int, string, Task>>(),
                It.IsAny<CancellationToken>()))
            .Callback<DeletionTargetType, int, int, Func<int, string, Task>, CancellationToken>(
                (_, _, bs, _, _) => observedBatchSize = bs)
            .Returns(Task.CompletedTask);

        int processed = await _service.ProcessPendingJobsAsync(0);

        Assert.That(processed, Is.EqualTo(1));
        Assert.That(observedBatchSize, Is.EqualTo(IDeletionJobService.DefaultBatchSize));
        Assert.That(job.Status, Is.EqualTo(DeletionJobStatus.Completed));
    }

    [Test]
    public async Task ProcessPendingJobsAsync_WhenChunkedDeleteThrows_MarksJobFailedAndContinues()
    {
        var failingJob = new DeletionJobBuilder().WithTarget(DeletionTargetType.Course, 1, "FailCourse").Build();
        var healthyJob = new DeletionJobBuilder().WithTarget(DeletionTargetType.Period, 2, "OkPeriod").Build();

        _jobRepositoryMock.Setup(r => r.GetActiveAsync(null)).ReturnsAsync([failingJob, healthyJob]);
        _chunkedDeletionRepositoryMock
            .Setup(r => r.CountDependentAndRootRowsAsync(DeletionTargetType.Course, 1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SQL failure"));
        _chunkedDeletionRepositoryMock
            .Setup(r => r.CountDependentAndRootRowsAsync(DeletionTargetType.Period, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        int processed = await _service.ProcessPendingJobsAsync(500);

        Assert.That(processed, Is.EqualTo(1));
        Assert.That(failingJob.Status, Is.EqualTo(DeletionJobStatus.Failed));
        Assert.That(failingJob.ErrorMessage, Does.Contain("SQL failure"));
        Assert.That(healthyJob.Status, Is.EqualTo(DeletionJobStatus.Completed));
    }
}
