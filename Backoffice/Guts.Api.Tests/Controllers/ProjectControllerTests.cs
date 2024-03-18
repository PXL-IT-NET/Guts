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
using Microsoft.Extensions.Caching.Memory;
using Moq;
using NUnit.Framework;

namespace Guts.Api.Tests.Controllers
{
    [TestFixture]
    public class ProjectControllerTests
    {
        private Random _random;
        private int _userId;
        private ProjectController _controller;
        private Mock<IProjectService> _projectServiceMock;
        private Mock<IProjectConverter> _projectConverterMock;
        private Mock<ITeamConverter> _teamConverterMock;
        private Mock<IMemoryCache> _memoryCacheMock;
        private Mock<ITopicConverter> _topicConverterMock;
        private Mock<IAssignmentRepository> _assignmentRepositoryMock;
        private Mock<ISolutionFileService> _solutionFileServiceMock;

        [SetUp]
        public void Setup()
        {
            _random = new Random();
            _userId = _random.NextPositive();
            _projectServiceMock = new Mock<IProjectService>();
            _assignmentRepositoryMock = new Mock<IAssignmentRepository>();
            _projectConverterMock = new Mock<IProjectConverter>();
            _topicConverterMock = new Mock<ITopicConverter>();
            _teamConverterMock = new Mock<ITeamConverter>();
            _solutionFileServiceMock = new Mock<ISolutionFileService>();
            _memoryCacheMock = new Mock<IMemoryCache>();
            _controller = CreateControllerWithUserInContext(Role.Constants.Student);
        }

        [Test]
        [TestCase(-1, "validCode")]
        [TestCase(1, null)]
        [TestCase(1, "")]
        public void GetProjectDetails_ShouldReturnBadRequestOnInvalidInput(int courseId, string projectCode)
        {
            //Act
            var badRequestResult = _controller.GetProjectDetails(courseId, projectCode).Result as BadRequestResult;

            //Assert
            Assert.That(badRequestResult, Is.Not.Null);
        }

        [Test]
        public void GetProjectDetails_Lector_ShouldReturnComponentsAndAllTeamsOfProject()
        {
            //Arrange
            var courseId = _random.NextPositive();
            var projectCode = Guid.NewGuid().ToString();
            _controller = CreateControllerWithUserInContext(Role.Constants.Lector);

            var project = new ProjectBuilder().WithId().Build();
            _projectServiceMock.Setup(service => service.LoadProjectAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(project);

            var model = new ProjectDetailModel();
            _projectConverterMock.Setup(converter => converter.ToProjectDetailModel(It.IsAny<Project>()))
                .Returns(model);

            //Act
            var okResult = _controller.GetProjectDetails(courseId, projectCode).Result as OkObjectResult;

            //Assert
            Assert.That(okResult, Is.Not.Null);
            _projectServiceMock.Verify(service => service.LoadProjectAsync(courseId, projectCode), Times.Once);
            _projectServiceMock.Verify(
                service => service.LoadProjectForUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()),
                Times.Never);
            _projectConverterMock.Verify(converter => converter.ToProjectDetailModel(project), Times.Once);
            Assert.That(okResult.Value, Is.EqualTo(model));
        }

        [Test]
        public void GetProjectDetails_Student_ShouldReturnComponentsAndOnlyOwnTeamOfProject()
        {
            //Arrange
            var courseId = _random.NextPositive();
            var projectCode = Guid.NewGuid().ToString();

            var project = new ProjectBuilder().WithId().Build();
            _projectServiceMock.Setup(service => service.LoadProjectForUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(project);

            var model = new ProjectDetailModel();
            _projectConverterMock.Setup(converter => converter.ToProjectDetailModel(It.IsAny<Project>()))
                .Returns(model);

            //Act
            var okResult = _controller.GetProjectDetails(courseId, projectCode).Result as OkObjectResult;

            //Assert
            Assert.That(okResult, Is.Not.Null);
            _projectServiceMock.Verify(service => service.LoadProjectForUserAsync(courseId, projectCode, _userId), Times.Once);
            _projectServiceMock.Verify(service => service.LoadProjectAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
            _projectConverterMock.Verify(converter => converter.ToProjectDetailModel(project), Times.Once);
            Assert.That(okResult.Value, Is.EqualTo(model));
        }

        [Test]
        [TestCase(-1, "validCode")]
        [TestCase(1, null)]
        [TestCase(1, "")]
        public void GetProjectTeams_ShouldReturnBadRequestOnInvalidInput(int courseId, string projectCode)
        {
            //Act
            var badRequestResult = _controller.GetProjectTeams(courseId, projectCode).Result as BadRequestResult;

            //Assert
            Assert.That(badRequestResult, Is.Not.Null);
        }

        [Test]
        public void GetProjectTeams_ShouldLoadTeamsOfProjectAndReturnThem()
        {
            //Arrange
            var courseId = _random.NextPositive();
            var projectCode = Guid.NewGuid().ToString();

            var teams = new List<ProjectTeam>
            {
                new ProjectTeamBuilder().Build(),
                new ProjectTeamBuilder().Build()
            };
            _projectServiceMock.Setup(service => service.LoadTeamsOfProjectAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(teams);


            _teamConverterMock.Setup(converter => converter.ToTeamDetailsModel(It.IsAny<ProjectTeam>()))
                .Returns(new TeamDetailsModel());

            //Act
            var okResult = _controller.GetProjectTeams(courseId, projectCode).Result as OkObjectResult;

            //Assert
            Assert.That(okResult, Is.Not.Null);
            _projectServiceMock.Verify(service => service.LoadTeamsOfProjectAsync(courseId, projectCode), Times.Once);
            _teamConverterMock.Verify(converter => converter.ToTeamDetailsModel(It.IsIn<ProjectTeam>(teams)), Times.Exactly(teams.Count));
            Assert.That(okResult.Value, Has.Count.EqualTo(teams.Count));
            Assert.That(okResult.Value, Has.All.TypeOf<TeamDetailsModel>());
        }

        [Test]
        public void LeaveProjectTeam_ShouldUseServiceForLoggedInUser()
        {
            //Arrange
            var courseId = _random.NextPositive();
            var projectCode = Guid.NewGuid().ToString();
            var teamId = _random.NextPositive();

            //Act
            var okResult = _controller.LeaveProjectTeam(courseId, projectCode, teamId).Result as OkResult;

            //Assert
            Assert.That(okResult, Is.Not.Null);
            _projectServiceMock.Verify(service => service.RemoveUserFromProjectTeamAsync(courseId, projectCode, teamId, _userId), Times.Once);
        }

        [Test]
        public void RemoveFromProjectTeam_ShouldUseService()
        {
            //Arrange
            var courseId = _random.NextPositive();
            var projectCode = Guid.NewGuid().ToString();
            var teamId = _random.NextPositive();
            var userId = _random.NextPositive();

            //Act
            var okResult = _controller.RemoveFromProjectTeam(courseId, projectCode, teamId, userId).Result as OkResult;

            //Assert
            Assert.That(okResult, Is.Not.Null);
            _projectServiceMock.Verify(service => service.RemoveUserFromProjectTeamAsync(courseId, projectCode, teamId, userId), Times.Once);
        }

        private ProjectController CreateControllerWithUserInContext(string role)
        {
            return new ProjectController(_projectServiceMock.Object, 
                null, //TODO: add mock
                _assignmentRepositoryMock.Object,
                _projectConverterMock.Object, 
                _topicConverterMock.Object, 
                _teamConverterMock.Object,
                _solutionFileServiceMock.Object,
                _memoryCacheMock.Object)
            {
                ControllerContext = new ControllerContextBuilder().WithUser(_userId.ToString()).WithRole(role).Build()
            };
        }
    }
}
