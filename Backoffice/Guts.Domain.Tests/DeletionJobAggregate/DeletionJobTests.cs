using System;
using Guts.Common;
using Guts.Domain.DeletionJobAggregate;
using Guts.Domain.Tests.Builders;
using NUnit.Framework;

namespace Guts.Domain.Tests.DeletionJobAggregate;

[TestFixture]
public class DeletionJobTests
{
    [Test]
    public void Start_FromPending_TransitionsToInProgress()
    {
        var job = new DeletionJobBuilder().Build();
        var startedAt = DateTime.UtcNow;

        job.Start(500, startedAt, "  Deleting test results  ");

        Assert.That(job.Status, Is.EqualTo(DeletionJobStatus.InProgress));
        Assert.That(job.TotalRows, Is.EqualTo(500));
        Assert.That(job.DeletedRows, Is.Zero);
        Assert.That(job.StartedAtUtc, Is.EqualTo(startedAt));
        Assert.That(job.CurrentPhase, Is.EqualTo("Deleting test results"));
    }

    [Test]
    public void Start_WhenNotPending_OrInvalidInputs_ThrowsContractException()
    {
        var job = new DeletionJobBuilder().Build();
        Assert.Throws<ContractException>(() => job.Start(0, DateTime.UtcNow, "Phase"));
        Assert.Throws<ContractException>(() => job.Start(10, DateTime.UtcNow, " "));

        job.Start(10, DateTime.UtcNow, "Phase");
        Assert.Throws<ContractException>(() => job.Start(10, DateTime.UtcNow, "Again"));
    }

    [Test]
    public void RecordBatchProgress_AccumulatesAndCapsAtTotalRowsAnd99Percent()
    {
        var job = new DeletionJobBuilder().Build();
        job.Start(100, DateTime.UtcNow, "Starting");

        job.RecordBatchProgress(50, "Deleting solution files");
        Assert.That(job.DeletedRows, Is.EqualTo(50));
        Assert.That(job.CurrentPhase, Is.EqualTo("Deleting solution files"));
        Assert.That(job.CalculatePercentage(), Is.EqualTo(50));

        // Even if batch slightly exceeds initial count before root deletion, percentage caps at 99 until Completed
        job.RecordBatchProgress(80, "Deleting assignments");
        Assert.That(job.DeletedRows, Is.EqualTo(100));
        Assert.That(job.CalculatePercentage(), Is.EqualTo(99));
    }

    [Test]
    public void RecordBatchProgress_InvalidStateOrArguments_ThrowsContractException()
    {
        var pendingJob = new DeletionJobBuilder().Build();
        Assert.Throws<ContractException>(() => pendingJob.RecordBatchProgress(5, "Phase"));

        pendingJob.Start(10, DateTime.UtcNow, "Phase");
        Assert.Throws<ContractException>(() => pendingJob.RecordBatchProgress(-1, "Phase"));
        Assert.Throws<ContractException>(() => pendingJob.RecordBatchProgress(1, " "));
    }

    [Test]
    public void ResetForResume_FromPendingOrInProgress_ResetsCountsAndMovesToInProgress()
    {
        var job = new DeletionJobBuilder().Build();
        job.Start(100, DateTime.UtcNow.AddMinutes(-5), "Old phase");
        job.RecordBatchProgress(40, "Old phase");

        var resumeTime = DateTime.UtcNow;
        job.ResetForResume(60, resumeTime, "Resuming cleanup");

        Assert.That(job.Status, Is.EqualTo(DeletionJobStatus.InProgress));
        Assert.That(job.TotalRows, Is.EqualTo(60));
        Assert.That(job.DeletedRows, Is.Zero);
        Assert.That(job.CurrentPhase, Is.EqualTo("Resuming cleanup"));
        Assert.That(job.StartedAtUtc, Is.Not.Null);

        var pendingJob = new DeletionJobBuilder().Build();
        pendingJob.ResetForResume(0, resumeTime, "Starting");
        Assert.That(pendingJob.Status, Is.EqualTo(DeletionJobStatus.InProgress));
        Assert.That(pendingJob.TotalRows, Is.EqualTo(1));
        Assert.That(pendingJob.StartedAtUtc, Is.EqualTo(resumeTime));
    }

    [Test]
    public void ResetForResume_WhenCompletedOrFailed_ThrowsContractException()
    {
        var completed = new DeletionJobBuilder().Build();
        completed.Start(10, DateTime.UtcNow, "Phase");
        completed.Complete(DateTime.UtcNow);
        Assert.Throws<ContractException>(() => completed.ResetForResume(5, DateTime.UtcNow, "Phase"));
        Assert.Throws<ContractException>(() => new DeletionJobBuilder().Build().ResetForResume(5, DateTime.UtcNow, ""));
    }

    [Test]
    public void Complete_FromInProgress_SetsCompletedAnd100Percent()
    {
        var job = new DeletionJobBuilder().Build();
        job.Start(25, DateTime.UtcNow, "Working");
        var completedAt = DateTime.UtcNow.AddSeconds(10);

        job.Complete(completedAt);

        Assert.That(job.Status, Is.EqualTo(DeletionJobStatus.Completed));
        Assert.That(job.DeletedRows, Is.EqualTo(25));
        Assert.That(job.CurrentPhase, Is.EqualTo("Completed"));
        Assert.That(job.CompletedAtUtc, Is.EqualTo(completedAt));
        Assert.That(job.CalculatePercentage(), Is.EqualTo(100));
    }

    [Test]
    public void Complete_WhenNotInProgress_ThrowsContractException()
    {
        var pending = new DeletionJobBuilder().Build();
        Assert.Throws<ContractException>(() => pending.Complete(DateTime.UtcNow));
    }

    [Test]
    public void Fail_FromPendingOrInProgress_SetsFailedAndTruncatesLongMessage()
    {
        var job = new DeletionJobBuilder().Build();
        var failedAt = DateTime.UtcNow;

        job.Fail("  Database timeout  ", failedAt);

        Assert.That(job.Status, Is.EqualTo(DeletionJobStatus.Failed));
        Assert.That(job.ErrorMessage, Is.EqualTo("Database timeout"));
        Assert.That(job.CompletedAtUtc, Is.EqualTo(failedAt));

        var job2 = new DeletionJobBuilder().Build();
        job2.Start(10, DateTime.UtcNow, "Working");
        job2.Fail(new string('X', 1500), DateTime.UtcNow);
        Assert.That(job2.ErrorMessage.Length, Is.EqualTo(DeletionJob.MaxErrorMessageLength));
    }

    [Test]
    public void Fail_WhenAlreadyFinishedOrEmptyMessage_ThrowsContractException()
    {
        var job = new DeletionJobBuilder().Build();
        Assert.Throws<ContractException>(() => job.Fail(" ", DateTime.UtcNow));

        job.Start(10, DateTime.UtcNow, "Working");
        job.Complete(DateTime.UtcNow);
        Assert.Throws<ContractException>(() => job.Fail("Error", DateTime.UtcNow));
    }

    [Test]
    public void Equality_UsesId()
    {
        var sharedId = Guid.NewGuid();
        var jobA = new DeletionJobBuilder().WithId(sharedId).Build();
        var jobB = new DeletionJobBuilder().WithId(sharedId).Build();
        var jobC = new DeletionJobBuilder().WithId(Guid.NewGuid()).Build();

        Assert.That(jobA, Is.EqualTo(jobB));
        Assert.That(jobA, Is.Not.EqualTo(jobC));
    }
}
