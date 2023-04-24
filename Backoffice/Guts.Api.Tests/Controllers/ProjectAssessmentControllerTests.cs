using System;
using System.Collections.Generic;
using AutoMapper;
using Guts.Api.Controllers;
using Guts.Api.Models.ProjectModels;
using Guts.Api.Tests.Builders;
using Guts.Business.Repositories;
using Guts.Business.Services;
using Guts.Common.Extensions;
using Guts.Domain.RoleAggregate;
using Guts.Domain.Tests.Builders;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace Guts.Api.Tests.Controllers;

[TestFixture]
public class ProjectAssessmentControllerTests
{
    private Random _random;
    private int _userId;
    private ProjectAssessmentController _controller;
    private Mock<IProjectService> _projectServiceMock;
    private Mock<IProjectAssessmentRepository> _projectAssessmentRepositoryMock;
    private Mock<IMapper> _mapperMock;

    [SetUp]
    public void Setup()
    {
        _random = new Random();
        _userId = _random.NextPositive();
        _projectServiceMock = new Mock<IProjectService>();
        _projectAssessmentRepositoryMock = new Mock<IProjectAssessmentRepository>();
        _mapperMock = new Mock<IMapper>();
        _controller = CreateControllerWithUserInContext(Role.Constants.Lector);
    }

    [Test]
    public void GetProjectAssessments_ShouldMapResultsRetrievedFromRepository()
    {
        //Arrange
        int projectId = _random.NextPositive();

        var existingAssessments = new List<IProjectAssessment>
        {
            new ProjectAssessmentMockBuilder().Build().Object,
            new ProjectAssessmentMockBuilder().Build().Object,
            new ProjectAssessmentMockBuilder().Build().Object
        };

        _projectAssessmentRepositoryMock.Setup(repo => repo.FindByProjectIdAsync(projectId))
            .ReturnsAsync(existingAssessments);

        var model = new ProjectAssessmentModel();
        _mapperMock.Setup(mapper => mapper.Map<ProjectAssessmentModel>(It.IsAny<object>())).Returns(model);

        //Act
        OkObjectResult result = _controller.GetProjectAssessments(projectId).Result as OkObjectResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        var returnedModels = result.Value as IList<ProjectAssessmentModel>;
        Assert.That(returnedModels, Is.Not.Null);
        Assert.That(returnedModels, Has.Count.EqualTo(existingAssessments.Count));
        foreach (IProjectAssessment existingAssessment in existingAssessments)
        {
            _mapperMock.Verify(mapper => mapper.Map<ProjectAssessmentModel>(existingAssessment), Times.Once);
        }
    }

    [Test]
    public void GetProjectAssessment_ShouldMapResultRetrievedFromRepository()
    {
        //Arrange
        IProjectAssessment existingAssessment = new ProjectAssessmentMockBuilder().WithId().Build().Object;

        _projectAssessmentRepositoryMock.Setup(repo => repo.GetByIdAsync(existingAssessment.Id))
            .ReturnsAsync(existingAssessment);

        var model = new ProjectAssessmentModel();
        _mapperMock.Setup(mapper => mapper.Map<ProjectAssessmentModel>(It.IsAny<object>())).Returns(model);

        //Act
        OkObjectResult result = _controller.GetProjectAssessment(existingAssessment.Id).Result as OkObjectResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        var returnedModel = result.Value as ProjectAssessmentModel;
        Assert.That(returnedModel, Is.Not.Null);
        _mapperMock.Verify(mapper => mapper.Map<ProjectAssessmentModel>(existingAssessment), Times.Once);
    }

    [Test]
    public void AddProjectAssessment_ShouldUseServiceAndMapResult()
    {
        //Arrange
        IProjectAssessment createdAssessment = new ProjectAssessmentMockBuilder().WithId().Build().Object;

        _projectServiceMock
            .Setup(service => service.CreateProjectAssessmentAsync(It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(createdAssessment);

        var outputModel = new ProjectAssessmentModel();
        _mapperMock.Setup(mapper => mapper.Map<ProjectAssessmentModel>(createdAssessment)).Returns(outputModel);

        var inputModel = new CreateProjectAssessmentModel
        {
            ProjectId = _random.NextPositive(),
            Description = _random.NextString(),
            OpenOn = _random.NextDateTimeInFuture()
        };
        inputModel.Deadline = inputModel.OpenOn.AddDays(1);

        //Act
        CreatedAtActionResult result = _controller.AddProjectAssessment(inputModel).Result as CreatedAtActionResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        var returnedModel = result.Value as ProjectAssessmentModel;
        Assert.That(returnedModel, Is.SameAs(outputModel));
        Assert.That(result.ActionName, Is.EqualTo(nameof(ProjectAssessmentController.GetProjectAssessment)));
        Assert.That(result.RouteValues["id"], Is.EqualTo(createdAssessment.Id));

        _projectServiceMock.Verify(
            service => service.CreateProjectAssessmentAsync(inputModel.ProjectId, inputModel.Description,
                inputModel.OpenOn.ToUniversalTime(), inputModel.Deadline.ToUniversalTime()), Times.Once);
    }

    private ProjectAssessmentController CreateControllerWithUserInContext(string role)
    {
        return new ProjectAssessmentController(_projectServiceMock.Object, _projectAssessmentRepositoryMock.Object, _mapperMock.Object)
        {
            ControllerContext = new ControllerContextBuilder().WithUser(_userId.ToString()).WithRole(role).Build()
        };
    }
}