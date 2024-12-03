using System;
using System.Collections.Generic;
using Guts.Api.Controllers;
using Guts.Api.Models.Converters;
using Guts.Api.Models.ProjectModels;
using Guts.Api.Tests.Builders;
using Guts.Business.Repositories;
using Guts.Business.Services;
using Guts.Business.Tests.Builders;
using Guts.Common.Extensions;
using Guts.Domain.ProjectTeamAggregate;
using Guts.Domain.RoleAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace Guts.Api.Tests.Controllers;

[TestFixture]
public class ProjectTeamControllerTests
{
    private int _userId;
    private ProjectTeamController _controller;
    private Mock<IProjectService> _projectServiceMock;
    private Mock<ITeamConverter> _teamConverterMock;
    private Mock<ITopicConverter> _topicConverterMock;
    private Mock<IAssignmentRepository> _assignmentRepositoryMock;
    private Mock<IProjectTeamRepository> _projectTeamRepositoryMock;

    [SetUp]
    public void Setup()
    {
        _userId = Random.Shared.NextPositive();
        _projectTeamRepositoryMock = new Mock<IProjectTeamRepository>();
        _projectServiceMock = new Mock<IProjectService>();
        _assignmentRepositoryMock = new Mock<IAssignmentRepository>();
        _topicConverterMock = new Mock<ITopicConverter>();
        _teamConverterMock = new Mock<ITeamConverter>();
        _controller = CreateControllerWithUserInContext(Role.Constants.Student);
    }

    [Test]
    public void GetProjectTeams_ShouldLoadTeamsOfProjectAndReturnThem()
    {
        //Arrange
        int courseId = Random.Shared.NextPositive();
        int periodId = Random.Shared.NextPositive();
        string projectCode = Guid.NewGuid().ToString();

        List<ProjectTeam> teams = new List<ProjectTeam>
        {
            new ProjectTeamBuilder().Build(),
            new ProjectTeamBuilder().Build()
        };
        _projectServiceMock.Setup(service => service.LoadTeamsOfProjectAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>()))
            .ReturnsAsync(teams);


        _teamConverterMock.Setup(converter => converter.ToTeamDetailsModel(It.IsAny<ProjectTeam>()))
            .Returns(new TeamDetailsModel());

        //Act
        OkObjectResult okResult = _controller.GetProjectTeams(courseId, projectCode, periodId).Result as OkObjectResult;

        //Assert
        Assert.That(okResult, Is.Not.Null);
        _projectServiceMock.Verify(service => service.LoadTeamsOfProjectAsync(courseId, projectCode, periodId), Times.Once);
        _teamConverterMock.Verify(converter => converter.ToTeamDetailsModel(It.IsIn<ProjectTeam>(teams)), Times.Exactly(teams.Count));
        Assert.That(okResult.Value, Has.Count.EqualTo(teams.Count));
        Assert.That(okResult.Value, Has.All.TypeOf<TeamDetailsModel>());
    }

    [Test]
    public void GetProjectTeam_ShouldGetTeamFromRepo()
    {
        //Arrange
        Project project = new ProjectBuilder().Build();
        ProjectTeam team = new ProjectTeamBuilder().WithProject(project).Build();

        _projectTeamRepositoryMock.Setup(repo => repo.LoadByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(team);

        TeamDetailsModel convertedModel = new TeamDetailsModel();
        _teamConverterMock.Setup(converter => converter.ToTeamDetailsModel(It.IsAny<ProjectTeam>()))
            .Returns(convertedModel);

        //Act
        OkObjectResult okResult = _controller.GetProjectTeam(project.CourseId, project.Code, team.Id).Result as OkObjectResult;

        //Assert
        Assert.That(okResult, Is.Not.Null);
        _projectTeamRepositoryMock.Verify(repo => repo.LoadByIdAsync(team.Id), Times.Once);
        _teamConverterMock.Verify(converter => converter.ToTeamDetailsModel(team), Times.Once);
        Assert.That(okResult.Value, Is.SameAs(convertedModel));
    }

    [Test]
    public void AddProjectTeam_ShouldUseService()
    {
        //Arrange
        Project project = new ProjectBuilder().Build();
        ProjectTeam createdTeam = new ProjectTeamBuilder().WithProject(project).Build();

        _projectServiceMock.Setup(service => service.AddProjectTeamAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(createdTeam);

        TeamDetailsModel convertedModel = new TeamDetailsModel
        {
            Id = createdTeam.Id
        };
        _teamConverterMock.Setup(converter => converter.ToTeamDetailsModel(It.IsAny<ProjectTeam>()))
            .Returns(convertedModel);

        TeamEditModel inputModel = new TeamEditModel
        {
            Name = createdTeam.Name
        };

        //Act
        CreatedAtActionResult result = _controller.AddProjectTeam(project.CourseId, project.Code, inputModel).Result as CreatedAtActionResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        _projectServiceMock.Verify(service => service.AddProjectTeamAsync(project.CourseId, project.Code, inputModel.Name), Times.Once);
        _teamConverterMock.Verify(converter => converter.ToTeamDetailsModel(createdTeam), Times.Once);
        Assert.That(result.Value, Is.SameAs(convertedModel));
        Assert.That(result.RouteValues["courseId"], Is.EqualTo(project.CourseId));
        Assert.That(result.RouteValues["projectCode"], Is.EqualTo(project.Code.Value));
        Assert.That(result.RouteValues["teamId"], Is.EqualTo(convertedModel.Id));
        Assert.That(result.ActionName, Is.EqualTo(nameof(ProjectTeamController.GetProjectTeam)));
    }

    [Test]
    public void UpdateProjectTeam_ShouldUseService()
    {
        //Arrange
        Project project = new ProjectBuilder().Build();
        ProjectTeam team = new ProjectTeamBuilder().WithProject(project).Build();

        _projectServiceMock.Setup(service => service.UpdateProjectTeamAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()));

        TeamEditModel inputModel = new TeamEditModel
        {
            Name = team.Name
        };

        //Act
        OkResult result = _controller.UpdateProjectTeam(project.CourseId, project.Code, team.Id, inputModel).Result as OkResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        _projectServiceMock.Verify(service => service.UpdateProjectTeamAsync(project.CourseId, project.Code, team.Id, inputModel.Name), Times.Once);
    }

    [Test]
    public void GenerateProjectTeams_ShouldUseService()
    {
        //Arrange
        Project project = new ProjectBuilder().Build();
        ProjectTeam team = new ProjectTeamBuilder().WithProject(project).Build();

        _projectServiceMock.Setup(service => service.GenerateTeamsForProject(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()));

        TeamGenerationModel inputModel = new TeamGenerationModel()
        {
            TeamBaseName = Random.Shared.NextString(),
            TeamNumberFrom = Random.Shared.Next(1, 500),
            TeamNumberTo = Random.Shared.Next(500, 1001)
        };

        //Act
        OkResult result = _controller.GenerateProjectTeams(project.CourseId, project.Code, inputModel).Result as OkResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        _projectServiceMock.Verify(
            service => service.GenerateTeamsForProject(project.CourseId, project.Code, inputModel.TeamBaseName,
                inputModel.TeamNumberFrom, inputModel.TeamNumberTo), Times.Once);
    }

    [Test]
    public void GenerateProjectTeams_InvalidInput_ShouldReturnBadRequest()
    {
        //Arrange
        Project project = new ProjectBuilder().Build();
        ProjectTeam team = new ProjectTeamBuilder().WithProject(project).Build();

        _projectServiceMock.Setup(service => service.GenerateTeamsForProject(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()));

        _controller.ModelState.AddModelError("TeamNumberFrom", "Invalid number");
        TeamGenerationModel inputModel = new TeamGenerationModel()
        {
            TeamBaseName = Random.Shared.NextString(),
            TeamNumberFrom = 0,
            TeamNumberTo = Random.Shared.Next(500, 1001)
        };

        //Act + Assert
        BadRequestObjectResult result = _controller.GenerateProjectTeams(project.CourseId, project.Code, inputModel).Result as BadRequestObjectResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        _projectServiceMock.Verify(
            service => service.GenerateTeamsForProject(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Test]
    public void JoinProjectTeam_ShouldUseService()
    {
        //Arrange
        Project project = new ProjectBuilder().Build();
        ProjectTeam team = new ProjectTeamBuilder().WithProject(project).Build();


        //Act
        OkResult result = _controller.JoinProjectTeam(project.CourseId, project.Code, team.Id).Result as OkResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        _projectServiceMock.Verify(
            service => service.AddUserToProjectTeamAsync(project.CourseId, project.Code, team.Id, _userId), Times.Once);
    }

    [Test]
    public void LeaveProjectTeam_ShouldUseServiceForLoggedInUser()
    {
        //Arrange
        int courseId = Random.Shared.NextPositive();
        string projectCode = Guid.NewGuid().ToString();
        int teamId = Random.Shared.NextPositive();

        //Act
        OkResult okResult = _controller.LeaveProjectTeam(courseId, projectCode, teamId).Result as OkResult;

        //Assert
        Assert.That(okResult, Is.Not.Null);
        _projectServiceMock.Verify(service => service.RemoveUserFromProjectTeamAsync(courseId, projectCode, teamId, _userId), Times.Once);
    }

    [Test]
    public void RemoveFromProjectTeam_ShouldUseService()
    {
        //Arrange
        int courseId = Random.Shared.NextPositive();
        string projectCode = Guid.NewGuid().ToString();
        int teamId = Random.Shared.NextPositive();
        int userId = Random.Shared.NextPositive();

        //Act
        OkResult okResult = _controller.RemoveFromProjectTeam(courseId, projectCode, teamId, userId).Result as OkResult;

        //Assert
        Assert.That(okResult, Is.Not.Null);
        _projectServiceMock.Verify(service => service.RemoveUserFromProjectTeamAsync(courseId, projectCode, teamId, userId), Times.Once);
    }

    [Test]
    public void DeleteProjectTeamAsync_ShouldUseService()
    {
        //Arrange
        Project project = new ProjectBuilder().Build();
        ProjectTeam team = new ProjectTeamBuilder().WithProject(project).Build();

        //Act
        OkResult result = _controller.DeleteProjectTeam(project.CourseId, project.Code, team.Id).Result as OkResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        _projectServiceMock.Verify(
            service => service.DeleteProjectTeamAsync(project.CourseId, project.Code, team.Id), Times.Once);
    }

    private ProjectTeamController CreateControllerWithUserInContext(string role)
    {
        return new ProjectTeamController(_projectServiceMock.Object,
            _projectTeamRepositoryMock.Object,
            _assignmentRepositoryMock.Object,
            _topicConverterMock.Object,
            _teamConverterMock.Object)
        {
            ControllerContext = new ControllerContextBuilder().WithUser(_userId.ToString()).WithRole(role).Build()
        };
    }
}