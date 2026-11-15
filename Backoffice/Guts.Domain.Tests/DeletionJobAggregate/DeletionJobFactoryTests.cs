using System;
using Guts.Common;
using Guts.Domain.DeletionJobAggregate;
using NUnit.Framework;

namespace Guts.Domain.Tests.DeletionJobAggregate;

[TestFixture]
public class DeletionJobFactoryTests
{
    private DeletionJobFactory _factory;

    [SetUp]
    public void SetUp()
    {
        _factory = new DeletionJobFactory();
    }

    [Test]
    public void CreateNew_ValidCourseDeletion_InitializesPendingJob()
    {
        var now = new DateTime(2026, 11, 15, 13, 0, 0, DateTimeKind.Utc);

        var job = _factory.CreateNew(DeletionTargetType.Course, 42, "  CS101 - Intro  ", now);

        Assert.That(job.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(job.TargetType, Is.EqualTo(DeletionTargetType.Course));
        Assert.That(job.TargetId, Is.EqualTo(42));
        Assert.That(job.TargetLabel, Is.EqualTo("CS101 - Intro"));
        Assert.That(job.Status, Is.EqualTo(DeletionJobStatus.Pending));
        Assert.That(job.TotalRows, Is.Zero);
        Assert.That(job.DeletedRows, Is.Zero);
        Assert.That(job.CurrentPhase, Is.EqualTo("Queued"));
        Assert.That(job.ErrorMessage, Is.Null);
        Assert.That(job.CreatedAtUtc, Is.EqualTo(now));
        Assert.That(job.StartedAtUtc, Is.Null);
        Assert.That(job.CompletedAtUtc, Is.Null);
        Assert.That(job.CalculatePercentage(), Is.Zero);
    }

    [TestCase(0)]
    [TestCase(-3)]
    public void CreateNew_NonPositiveTargetId_ThrowsContractException(int invalidId)
    {
        Assert.Throws<ContractException>(() =>
            _factory.CreateNew(DeletionTargetType.Course, invalidId, "CS101", DateTime.UtcNow));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void CreateNew_EmptyTargetLabel_ThrowsContractException(string invalidLabel)
    {
        Assert.Throws<ContractException>(() =>
            _factory.CreateNew(DeletionTargetType.Period, 1, invalidLabel, DateTime.UtcNow));
    }

    [Test]
    public void CreateNew_InvalidTargetType_ThrowsContractException()
    {
        Assert.Throws<ContractException>(() =>
            _factory.CreateNew((DeletionTargetType)999, 1, "Invalid", DateTime.UtcNow));
    }
}
