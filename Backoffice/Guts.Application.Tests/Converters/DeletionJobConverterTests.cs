using System;
using Guts.Application.Converters;
using Guts.Domain.DeletionJobAggregate;
using Guts.Domain.Tests.Builders;
using NUnit.Framework;

namespace Guts.Application.Tests.Converters;

[TestFixture]
public class DeletionJobConverterTests
{
    private DeletionJobConverter _converter;

    [SetUp]
    public void SetUp()
    {
        _converter = new DeletionJobConverter();
    }

    [Test]
    public void ToDto_NullJob_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _converter.ToDto(null));
    }

    [Test]
    public void ToDto_ValidJob_MapsAllFieldsIncludingCalculatedPercentage()
    {
        var job = new DeletionJobBuilder()
            .WithTarget(DeletionTargetType.Period, 12, "2024-2025")
            .Build();
        job.Start(200, new DateTime(2026, 11, 15, 13, 0, 0, DateTimeKind.Utc), "Deleting test runs");
        job.RecordBatchProgress(100, "Deleting test runs");

        var dto = _converter.ToDto(job);

        Assert.That(dto.JobId, Is.EqualTo(job.Id));
        Assert.That(dto.TargetType, Is.EqualTo(DeletionTargetType.Period));
        Assert.That(dto.TargetId, Is.EqualTo(12));
        Assert.That(dto.TargetLabel, Is.EqualTo("2024-2025"));
        Assert.That(dto.Status, Is.EqualTo(DeletionJobStatus.InProgress));
        Assert.That(dto.TotalRows, Is.EqualTo(200));
        Assert.That(dto.DeletedRows, Is.EqualTo(100));
        Assert.That(dto.Percentage, Is.EqualTo(50));
        Assert.That(dto.CurrentPhase, Is.EqualTo("Deleting test runs"));
        Assert.That(dto.ErrorMessage, Is.Null);
        Assert.That(dto.CreatedAtUtc, Is.EqualTo(job.CreatedAtUtc));
        Assert.That(dto.StartedAtUtc, Is.EqualTo(job.StartedAtUtc));
        Assert.That(dto.CompletedAtUtc, Is.Null);
    }
}
