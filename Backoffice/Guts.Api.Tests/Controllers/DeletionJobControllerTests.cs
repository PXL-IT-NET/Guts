using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Guts.Api.Controllers;
using Guts.Api.Models;
using Guts.Application;
using Guts.Application.Dtos;
using Guts.Application.Services.Deletion;
using Guts.Domain.DeletionJobAggregate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace Guts.Api.Tests.Controllers;

[TestFixture]
public class DeletionJobControllerTests
{
    private Mock<IDeletionJobService> _deletionJobServiceMock;
    private Mock<IMapper> _mapperMock;
    private DeletionJobController _controller;

    [SetUp]
    public void Setup()
    {
        _deletionJobServiceMock = new Mock<IDeletionJobService>();
        _mapperMock = new Mock<IMapper>();
        _controller = new DeletionJobController(_deletionJobServiceMock.Object, _mapperMock.Object);
    }

    [Test]
    public void Controller_ShouldRequireAdminOnlyPolicy()
    {
        var attr = typeof(DeletionJobController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.That(attr, Is.Not.Null);
        Assert.That(attr!.Policy, Is.EqualTo(ApiConstants.AdminOnlyPolicy));
    }

    [Test]
    public async Task GetJobStatus_ExistingJob_ReturnsOkWithMappedModel()
    {
        var jobId = Guid.NewGuid();
        var dto = new DeletionJobDto { JobId = jobId, Percentage = 50 };
        var model = new DeletionJobStatusModel { JobId = jobId, Percentage = 50 };
        _deletionJobServiceMock.Setup(s => s.GetJobStatusAsync(jobId)).ReturnsAsync(dto);
        _mapperMock.Setup(m => m.MapToDeletionJobStatusModel(dto)).Returns(model);

        var result = await _controller.GetJobStatus(jobId) as OkObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value, Is.SameAs(model));
    }

    [Test]
    public async Task GetJobStatus_EmptyGuidOrNotFound_ReturnsBadRequestOrNotFound()
    {
        Assert.That(await _controller.GetJobStatus(Guid.Empty), Is.InstanceOf<BadRequestResult>());

        var missingId = Guid.NewGuid();
        _deletionJobServiceMock.Setup(s => s.GetJobStatusAsync(missingId)).ThrowsAsync(new DataNotFoundException());

        Assert.That(await _controller.GetJobStatus(missingId), Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task GetActiveJobs_ValidFilters_ReturnsMappedModels()
    {
        var dto = new DeletionJobDto { JobId = Guid.NewGuid(), TargetType = DeletionTargetType.Course };
        var model = new DeletionJobStatusModel { JobId = dto.JobId, TargetType = "Course" };

        _deletionJobServiceMock.Setup(s => s.GetActiveJobsAsync(null)).ReturnsAsync([dto]);
        _deletionJobServiceMock.Setup(s => s.GetActiveJobsAsync(DeletionTargetType.Course)).ReturnsAsync([dto]);
        _mapperMock.Setup(m => m.MapToDeletionJobStatusModel(dto)).Returns(model);

        var allResult = await _controller.GetActiveJobs(null) as OkObjectResult;
        Assert.That(allResult?.Value as IList<DeletionJobStatusModel>, Has.Count.EqualTo(1));

        var filteredResult = await _controller.GetActiveJobs("course") as OkObjectResult;
        Assert.That(filteredResult?.Value as IList<DeletionJobStatusModel>, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task GetActiveJobs_InvalidTargetType_ReturnsBadRequest()
    {
        var result = await _controller.GetActiveJobs("unknown");

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }
}
