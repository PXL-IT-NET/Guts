using System;
using Guts.Api.Models;
using Guts.Application.Dtos;
using Guts.Domain.DeletionJobAggregate;
using NUnit.Framework;

namespace Guts.Api.Tests.Models;

[TestFixture]
internal class MapperTests
{
    private Mapper _mapper;

    [SetUp]
    public void Setup()
    {
        _mapper = new Mapper();
    }

    [Test]
    public void MapToDeletionJobStatusModel_NullSource_ReturnsNull()
    {
        DeletionJobStatusModel result = _mapper.MapToDeletionJobStatusModel(null);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void MapToDeletionJobStatusModel_ValidDto_MapsAllProperties()
    {
        var jobId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow.AddMinutes(-5);
        var startedAt = DateTime.UtcNow.AddMinutes(-4);
        var completedAt = DateTime.UtcNow;

        var dto = new DeletionJobDto
        {
            JobId = jobId,
            TargetType = DeletionTargetType.Course,
            TargetId = 42,
            TargetLabel = "NET1 - .NET Essentials",
            Status = DeletionJobStatus.Completed,
            TotalRows = 1200,
            DeletedRows = 1200,
            Percentage = 100,
            CurrentPhase = "Completed",
            ErrorMessage = null,
            CreatedAtUtc = createdAt,
            StartedAtUtc = startedAt,
            CompletedAtUtc = completedAt
        };

        DeletionJobStatusModel model = _mapper.MapToDeletionJobStatusModel(dto);

        Assert.That(model, Is.Not.Null);
        Assert.That(model.JobId, Is.EqualTo(jobId));
        Assert.That(model.TargetType, Is.EqualTo("Course"));
        Assert.That(model.TargetId, Is.EqualTo(42));
        Assert.That(model.TargetLabel, Is.EqualTo("NET1 - .NET Essentials"));
        Assert.That(model.Status, Is.EqualTo("Completed"));
        Assert.That(model.TotalRows, Is.EqualTo(1200));
        Assert.That(model.DeletedRows, Is.EqualTo(1200));
        Assert.That(model.Percentage, Is.EqualTo(100));
        Assert.That(model.CurrentPhase, Is.EqualTo("Completed"));
        Assert.That(model.ErrorMessage, Is.Null);
        Assert.That(model.CreatedAtUtc, Is.EqualTo(createdAt));
        Assert.That(model.StartedAtUtc, Is.EqualTo(startedAt));
        Assert.That(model.CompletedAtUtc, Is.EqualTo(completedAt));
    }
}
