using System;
using System.Collections.Generic;
using Guts.Api.Controllers;
using Guts.Api.Models;
using Guts.Api.Models.Converters;
using Guts.Api.Models.ProjectModels;
using Guts.Api.Tests.Builders;
using Guts.Business;
using Guts.Business.Dtos;
using Guts.Business.Services;
using Guts.Business.Tests.Builders;
using Guts.Common.Extensions;
using Guts.Domain.RoleAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using NUnit.Framework;

namespace Guts.Api.Tests.Controllers;

[TestFixture]
public class ProjectControllerTests
{
    private int _userId;
    private ProjectController _controller;
    private Mock<IProjectService> _projectServiceMock;
    private Mock<IProjectConverter> _projectConverterMock;
    private Mock<IMemoryCache> _memoryCacheMock;
    private Mock<ITopicConverter> _topicConverterMock;
    private Mock<ISolutionFileService> _solutionFileServiceMock;

    [SetUp]
    public void Setup()
    {
        _userId = Random.Shared.NextPositive();
        _projectServiceMock = new Mock<IProjectService>();
        _projectConverterMock = new Mock<IProjectConverter>();
        _topicConverterMock = new Mock<ITopicConverter>();
        _solutionFileServiceMock = new Mock<ISolutionFileService>();
        _memoryCacheMock = new Mock<IMemoryCache>();

        object cachedProjectStatisticsModel;
        _memoryCacheMock.Setup(cache => cache.TryGetValue(It.IsAny<object>(), out cachedProjectStatisticsModel)).Returns(false);

        _controller = CreateControllerWithUserInContext(Role.Constants.Student);
    }
    [Test]
    [TestCase(-1, "validCode")]
    [TestCase(1, null)]
    [TestCase(1, "")]
    public void GetProjectDetails_ShouldReturnBadRequestOnInvalidInput(int courseId, string projectCode)
    {
        //Act
        BadRequestResult badRequestResult = _controller.GetProjectDetails(courseId, projectCode).Result as BadRequestResult;

        //Assert
        Assert.That(badRequestResult, Is.Not.Null);
    }

    [Test]
    public void GetProjectDetails_Lector_ShouldReturnComponentsAndAllTeamsOfProject()
    {
        //Arrange
        int courseId = Random.Shared.NextPositive();
        int periodId = Random.Shared.NextPositive();
        string projectCode = Guid.NewGuid().ToString();
        _controller = CreateControllerWithUserInContext(Role.Constants.Lector);

        Project project = new ProjectBuilder().WithId().Build();
        _projectServiceMock.Setup(service => service.LoadProjectAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>()))
            .ReturnsAsync(project);

        ProjectDetailModel model = new ProjectDetailModel();
        _projectConverterMock.Setup(converter => converter.ToProjectDetailModel(It.IsAny<Project>()))
            .Returns(model);

        //Act
        OkObjectResult okResult = _controller.GetProjectDetails(courseId, projectCode, periodId).Result as OkObjectResult;

        //Assert
        Assert.That(okResult, Is.Not.Null);
        _projectServiceMock.Verify(service => service.LoadProjectAsync(courseId, projectCode, periodId), Times.Once);
        _projectServiceMock.Verify(
            service => service.LoadProjectForUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int?>()),
            Times.Never);
        _projectConverterMock.Verify(converter => converter.ToProjectDetailModel(project), Times.Once);
        Assert.That(okResult.Value, Is.EqualTo(model));
    }

    [Test]
    public void GetProjectDetails_Student_ShouldReturnComponentsAndOnlyOwnTeamOfProject()
    {
        //Arrange
        int courseId = Random.Shared.NextPositive();
        int periodId = Random.Shared.NextPositive();
        string projectCode = Guid.NewGuid().ToString();

        Project project = new ProjectBuilder().WithId().Build();
        _projectServiceMock.Setup(service => service.LoadProjectForUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int?>()))
            .ReturnsAsync(project);

        ProjectDetailModel model = new ProjectDetailModel();
        _projectConverterMock.Setup(converter => converter.ToProjectDetailModel(It.IsAny<Project>()))
            .Returns(model);

        //Act
        OkObjectResult okResult = _controller.GetProjectDetails(courseId, projectCode, periodId).Result as OkObjectResult;

        //Assert
        Assert.That(okResult, Is.Not.Null);
        _projectServiceMock.Verify(service => service.LoadProjectForUserAsync(courseId, projectCode, _userId, periodId), Times.Once);
        _projectServiceMock.Verify(service => service.LoadProjectAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>()), Times.Never);
        _projectConverterMock.Verify(converter => converter.ToProjectDetailModel(project), Times.Once);
        Assert.That(okResult.Value, Is.EqualTo(model));
    }

    [Test]
    [TestCase(0)]
    [TestCase(-1)]
    public void AddProject_WhenCourseIdIsInvalid_ShouldReturnBadRequest(int invalidCourseId)
    {
        //Arrange
        var model = new TopicAddModel
        {
            Code = Random.Shared.NextString(),
            Description = Random.Shared.NextString()
        };

        //Act
        BadRequestResult result = _controller.AddProject(invalidCourseId, model).Result as BadRequestResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        _projectServiceMock.Verify(service => service.CreateProjectAsync(It.IsAny<int>(), It.IsAny<Guts.Domain.ValueObjects.Code>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void AddProject_WhenInputIsValid_ShouldCreateProjectAndReturnCreatedAtAction()
    {
        //Arrange
        int courseId = Random.Shared.NextPositive();
        var model = new TopicAddModel
        {
            Code = Random.Shared.NextString(),
            Description = Random.Shared.NextString()
        };

        Project createdProject = new ProjectBuilder().WithId().Build();
        _projectServiceMock.Setup(service => service.CreateProjectAsync(courseId, model.Code, model.Description))
            .ReturnsAsync(createdProject);

        var detailModel = new ProjectDetailModel { Code = model.Code };
        _projectConverterMock.Setup(converter => converter.ToProjectDetailModel(createdProject))
            .Returns(detailModel);

        //Act
        CreatedAtActionResult result = _controller.AddProject(courseId, model).Result as CreatedAtActionResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ActionName, Is.EqualTo(nameof(ProjectController.GetProjectDetails)));
        Assert.That(result.RouteValues["courseId"], Is.EqualTo(courseId));
        Assert.That(result.RouteValues["projectCode"], Is.EqualTo(detailModel.Code));
        Assert.That(result.Value, Is.SameAs(detailModel));
        _projectServiceMock.Verify(service => service.CreateProjectAsync(courseId, model.Code, model.Description), Times.Once);
        _projectConverterMock.Verify(converter => converter.ToProjectDetailModel(createdProject), Times.Once);
    }

    [Test]
    [TestCase(0, "validCode")]
    [TestCase(-1, "validCode")]
    [TestCase(1, "")]
    [TestCase(1, null)]
    public void UpdateProject_WhenInputIsInvalid_ShouldReturnBadRequest(int courseId, string projectCode)
    {
        //Arrange
        var model = new TopicUpdateModel { Description = Random.Shared.NextString() };

        //Act
        BadRequestResult result = _controller.UpdateProject(courseId, projectCode, model).Result as BadRequestResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        _projectServiceMock.Verify(service => service.UpdateProjectAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void UpdateProject_WhenInputIsValid_ShouldUpdateProjectAndReturnOk()
    {
        //Arrange
        int courseId = Random.Shared.NextPositive();
        string projectCode = Guid.NewGuid().ToString();
        var model = new TopicUpdateModel { Description = Random.Shared.NextString() };

        //Act
        OkResult result = _controller.UpdateProject(courseId, projectCode, model).Result as OkResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        _projectServiceMock.Verify(service => service.UpdateProjectAsync(courseId, projectCode, model.Description), Times.Once);
    }

    [Test]
    [TestCase(0, "validCode")]
    [TestCase(-1, "validCode")]
    [TestCase(1, "")]
    [TestCase(1, null)]
    public void GetProjectStatistics_WhenInputIsInvalid_ShouldReturnBadRequest(int courseId, string projectCode)
    {
        //Act
        BadRequestResult result = _controller.GetProjectStatistics(courseId, projectCode, null, null).Result as BadRequestResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        _projectServiceMock.Verify(service => service.LoadProjectAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>()), Times.Never);
    }

    [Test]
    public void GetProjectStatistics_WhenInputIsValid_ShouldReturnStatistics()
    {
        //Arrange
        Project existingProject = new ProjectBuilder().WithId().Build();
        var projectStatistics = new List<AssignmentStatisticsDto>();
        var statisticsModel = new TopicStatisticsModel();
        DateTime date = DateTime.UtcNow.AddDays(-1);
        int periodId = Random.Shared.NextPositive();

        _projectServiceMock.Setup(service => service.LoadProjectAsync(existingProject.CourseId, existingProject.Code, periodId))
            .ReturnsAsync(existingProject);
        _projectServiceMock.Setup(service => service.GetProjectStatisticsAsync(existingProject, date.ToUniversalTime()))
            .ReturnsAsync(projectStatistics);
        _topicConverterMock.Setup(converter => converter.ToTopicStatisticsModel(existingProject, projectStatistics, "Teams"))
            .Returns(statisticsModel);

        //Act
        OkObjectResult result = _controller.GetProjectStatistics(existingProject.CourseId, existingProject.Code, date, periodId).Result as OkObjectResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Is.SameAs(statisticsModel));
        _projectServiceMock.Verify(service => service.LoadProjectAsync(existingProject.CourseId, existingProject.Code, periodId), Times.Once);
        _projectServiceMock.Verify(service => service.GetProjectStatisticsAsync(existingProject, date.ToUniversalTime()), Times.Once);
        _topicConverterMock.Verify(converter => converter.ToTopicStatisticsModel(existingProject, projectStatistics, "Teams"), Times.Once);
    }

    [Test]
    public void GetProjectStatistics_WhenDateIsRecentAndCacheHit_ShouldReturnCachedStatistics()
    {
        //Arrange
        int courseId = Random.Shared.NextPositive();
        string projectCode = Guid.NewGuid().ToString();
        var cachedModel = new TopicStatisticsModel();
        object outValue = cachedModel;

        _memoryCacheMock
            .Setup(cache => cache.TryGetValue($"GetProjectStatistics-{courseId}-{projectCode}-0", out outValue))
            .Returns(true);

        //Act
        OkObjectResult result = _controller.GetProjectStatistics(courseId, projectCode, null, null).Result as OkObjectResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Is.SameAs(cachedModel));
        _projectServiceMock.Verify(service => service.LoadProjectAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>()), Times.Never);
    }

    [Test]
    public void GetProjectStatistics_WhenProjectNotFound_ShouldReturnNotFound()
    {
        //Arrange
        int courseId = Random.Shared.NextPositive();
        string projectCode = Guid.NewGuid().ToString();
        int periodId = Random.Shared.NextPositive();

        _projectServiceMock.Setup(service => service.LoadProjectAsync(courseId, projectCode, periodId))
            .ThrowsAsync(new DataNotFoundException());

        //Act
        NotFoundResult result = _controller.GetProjectStatistics(courseId, projectCode, null, periodId).Result as NotFoundResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        _projectServiceMock.Verify(service => service.LoadProjectAsync(courseId, projectCode, periodId), Times.Once);
    }

    [Test]
    public void DownloadSourceCodesAsZip_ShouldReturnZipFileContentResult()
    {
        //Arrange
        int courseId = Random.Shared.NextPositive();
        string projectCode = Guid.NewGuid().ToString();
        DateTime date = DateTime.UtcNow;
        Project project = new ProjectBuilder().WithId().Build();
        var solutions = new List<SolutionDto> { new SolutionDto() };
        var zipBytes = new byte[] { 10, 20, 30 };

        _projectServiceMock.Setup(service => service.LoadProjectAsync(courseId, projectCode, null))
            .ReturnsAsync(project);
        _projectServiceMock.Setup(service => service.GetAllSolutions(project, date))
            .ReturnsAsync(solutions);
        _solutionFileServiceMock.Setup(service => service.CreateZipFromFiles(solutions))
            .ReturnsAsync(zipBytes);

        //Act
        var result = _controller.DownloadSourceCodesAsZip(courseId, projectCode, date).Result as FileContentResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ContentType, Is.EqualTo("application/zip"));
        Assert.That(result.FileContents, Is.EqualTo(zipBytes));
        Assert.That(result.FileDownloadName, Is.EqualTo($"Project_{projectCode}_sources.zip"));
        _projectServiceMock.Verify(service => service.LoadProjectAsync(courseId, projectCode, null), Times.Once);
        _projectServiceMock.Verify(service => service.GetAllSolutions(project, date), Times.Once);
        _solutionFileServiceMock.Verify(service => service.CreateZipFromFiles(solutions), Times.Once);
    }

    private ProjectController CreateControllerWithUserInContext(string role)
    {
        return new ProjectController(_projectServiceMock.Object, 
            _projectConverterMock.Object, 
            _topicConverterMock.Object, 
            _solutionFileServiceMock.Object,
            _memoryCacheMock.Object)
        {
            ControllerContext = new ControllerContextBuilder().WithUser(_userId.ToString()).WithRole(role).Build()
        };
    }
}