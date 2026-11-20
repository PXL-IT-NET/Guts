using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Guts.Application.Repositories;
using Guts.Application.Services;
using Guts.Common;
using Guts.Domain.PeriodAggregate;
using Guts.Domain.TestRunAggregate;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Guts.Application.Tests.Services;

[TestFixture]
public class TestRunPruningServiceTests
{
    private Mock<IPeriodRepository> _periodRepositoryMock;
    private Mock<ITestRunRepository> _testRunRepositoryMock;
    private Mock<ILogger<TestRunPruningService>> _loggerMock;
    private TestRunPruningService _service;

    [SetUp]
    public void SetUp()
    {
        _periodRepositoryMock = new Mock<IPeriodRepository>();
        _testRunRepositoryMock = new Mock<ITestRunRepository>();
        _loggerMock = new Mock<ILogger<TestRunPruningService>>();
        _loggerMock.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        _service = new TestRunPruningService(
            _periodRepositoryMock.Object,
            _testRunRepositoryMock.Object,
            _loggerMock.Object);
    }

    [TestCase(0)]
    [TestCase(-10)]
    public void PruneRedundantTestRunsAsync_WhenBatchSizeNotPositive_ShouldThrowContractException(int batchSize)
    {
        Assert.ThrowsAsync<ContractException>(() => _service.PruneRedundantTestRunsAsync(batchSize));
    }

    [Test]
    public async Task PruneRedundantTestRunsAsync_WhenNoActivePeriodOrPeriodDeleting_ShouldReturnZero()
    {
        _periodRepositoryMock.Setup(r => r.GetPeriodAsync(null)).ThrowsAsync(new DataNotFoundException());
        var resultNotFound = await _service.PruneRedundantTestRunsAsync();
        Assert.That(resultNotFound, Is.EqualTo(new Dtos.TestRunPruningResult(0, 0, 0)));

        _periodRepositoryMock.Setup(r => r.GetPeriodAsync(null)).ReturnsAsync((IPeriod)null);
        var resultNull = await _service.PruneRedundantTestRunsAsync();
        Assert.That(resultNull, Is.EqualTo(new Dtos.TestRunPruningResult(0, 0, 0)));

        var deletingPeriod = Mock.Of<IPeriod>(p => p.Id == 5 && p.IsDeleting);
        _periodRepositoryMock.Setup(r => r.GetPeriodAsync(null)).ReturnsAsync(deletingPeriod);
        var resultDeleting = await _service.PruneRedundantTestRunsAsync();
        Assert.That(resultDeleting, Is.EqualTo(new Dtos.TestRunPruningResult(0, 0, 0)));

        _testRunRepositoryMock.Verify(
            r => r.GetPruningAssignmentTargetsForPeriodAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task PruneRedundantTestRunsAsync_ShouldScanChapterAndProjectAssignmentsCacheTeamsAndBatchDelete()
    {
        var activePeriod = Mock.Of<IPeriod>(p => p.Id == 7 && !p.IsDeleting);
        _periodRepositoryMock.Setup(r => r.GetPeriodAsync(null)).ReturnsAsync(activePeriod);

        var targets = new List<PruningAssignmentTarget>
        {
            new(101, 10, IsProject: false), // Chapter assignment with 4 runs (3 redundant -> 2 batches of size 2)
            new(102, 20, IsProject: true),  // Project 20, component 1 (team interleaved -> 0 redundant)
            new(103, 20, IsProject: true),  // Project 20, component 2 (reuses cached team map, 1 redundant)
            new(104, 10, IsProject: false)  // Single run snapshot -> skipped
        };
        _testRunRepositoryMock
            .Setup(r => r.GetPruningAssignmentTargetsForPeriodAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targets);

        var t0 = new DateTime(2026, 11, 20, 9, 0, 0, DateTimeKind.Utc);
        _testRunRepositoryMock
            .Setup(r => r.GetTestRunSnapshotsForAssignmentAsync(101, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TestRunSnapshot>
            {
                new(1, 1, t0, [new TestResultSnapshot(1, true)]),
                new(2, 1, t0.AddMinutes(1), [new TestResultSnapshot(1, true)]),
                new(3, 1, t0.AddMinutes(2), [new TestResultSnapshot(1, true)]),
                new(4, 1, t0.AddMinutes(3), [new TestResultSnapshot(1, true)])
            });

        _testRunRepositoryMock
            .Setup(r => r.GetProjectTeamMembershipMapAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, int> { [1] = 500, [2] = 500 });

        _testRunRepositoryMock
            .Setup(r => r.GetTestRunSnapshotsForAssignmentAsync(102, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TestRunSnapshot>
            {
                new(10, 1, t0, [new TestResultSnapshot(2, true)]),
                new(11, 2, t0.AddMinutes(1), [new TestResultSnapshot(2, false)]),
                new(12, 1, t0.AddMinutes(2), [new TestResultSnapshot(2, true)])
            });

        _testRunRepositoryMock
            .Setup(r => r.GetTestRunSnapshotsForAssignmentAsync(103, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TestRunSnapshot>
            {
                new(20, 1, t0, [new TestResultSnapshot(3, true)]),
                new(21, 2, t0.AddMinutes(1), [new TestResultSnapshot(3, true)])
            });

        _testRunRepositoryMock
            .Setup(r => r.GetTestRunSnapshotsForAssignmentAsync(104, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TestRunSnapshot>
            {
                new(30, 1, t0, [new TestResultSnapshot(4, true)])
            });

        var deletedBatches = new List<int[]>();
        _testRunRepositoryMock
            .Setup(r => r.DeleteTestRunsBatchAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<int>, CancellationToken>((ids, _) => deletedBatches.Add(ids.ToArray()))
            .ReturnsAsync((IReadOnlyCollection<int> ids, CancellationToken _) => new TestRunBatchDeletionResult(ids.Count, ids.Count * 2));

        var result = await _service.PruneRedundantTestRunsAsync(batchSize: 2);

        Assert.That(result.AssignmentsScanned, Is.EqualTo(4));
        Assert.That(result.TestRunsDeleted, Is.EqualTo(4));
        Assert.That(result.TestResultsDeleted, Is.EqualTo(8));
        Assert.That(deletedBatches.Count, Is.EqualTo(3)); // [2,3], [4], [21]
        Assert.That(deletedBatches[0], Is.EqualTo(new[] { 2, 3 }));
        Assert.That(deletedBatches[1], Is.EqualTo(new[] { 4 }));
        Assert.That(deletedBatches[2], Is.EqualTo(new[] { 21 }));

        // Team map for project 20 must only be queried once across both components
        _testRunRepositoryMock.Verify(
            r => r.GetProjectTeamMembershipMapAsync(20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task PruneRedundantTestRunsAsync_WhenCancelled_ShouldThrowOperationCanceledException()
    {
        var activePeriod = Mock.Of<IPeriod>(p => p.Id == 7 && !p.IsDeleting);
        _periodRepositoryMock.Setup(r => r.GetPeriodAsync(null)).ReturnsAsync(activePeriod);
        _testRunRepositoryMock
            .Setup(r => r.GetPruningAssignmentTargetsForPeriodAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PruningAssignmentTarget> { new(101, 10, false) });

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThatAsync(
            async () => await _service.PruneRedundantTestRunsAsync(500, cts.Token),
            Throws.TypeOf<OperationCanceledException>());
    }
}
