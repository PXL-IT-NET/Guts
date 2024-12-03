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
}
