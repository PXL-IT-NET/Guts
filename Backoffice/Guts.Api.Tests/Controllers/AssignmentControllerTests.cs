using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Guts.Api.Controllers;
using Guts.Api.Models.AssignmentModels;
using Guts.Api.Models.Converters;
using Guts.Api.Tests.Builders;
using Guts.Business.Dtos;
using Guts.Business.Repositories;
using Guts.Business.Services;
using Guts.Business.Tests.Builders;
using Guts.Common.Extensions;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.RoleAggregate;
using Guts.Domain.TestAggregate;
using Guts.Domain.TestRunAggregate;
using Guts.Domain.Tests.Builders;
using Guts.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using ControllerBase = Guts.Api.Controllers.ControllerBase;

namespace Guts.Api.Tests.Controllers
{
    [TestFixture]
    public class AssignmentControllerTests
    {
        private AssignmentController _controller;
        private Random _random;
        private Mock<IAssignmentService> _assignmentServiceMock;
        private Mock<IAssignmentRepository> _assignmentRepositoryMock;
        private Mock<IAssignmentConverter> _assignmentConverterMock;
        private Mock<IProjectTeamRepository> _projectTeamRepositoryMock;
        private Mock<ITopicService> _topicServiceMock;
        private Mock<IMapper> _mapperMock;
        private Mock<ISolutionFileRepository> _solutionFileRepositoryMock;
        private Mock<ISolutionFileService> _solutionFileServiceMock;

        [SetUp]
        public void Setup()
        {
            _random = new Random();
            _assignmentServiceMock = new Mock<IAssignmentService>();
            _assignmentRepositoryMock = new Mock<IAssignmentRepository>();
            _assignmentConverterMock = new Mock<IAssignmentConverter>();
            _projectTeamRepositoryMock = new Mock<IProjectTeamRepository>();
            _topicServiceMock = new Mock<ITopicService>();
            _solutionFileRepositoryMock = new Mock<ISolutionFileRepository>();
            _solutionFileServiceMock = new Mock<ISolutionFileService>();
            _mapperMock = new Mock<IMapper>();

            _controller = new AssignmentController(_assignmentServiceMock.Object,
                _assignmentRepositoryMock.Object, 
                _assignmentConverterMock.Object,
                _projectTeamRepositoryMock.Object, 
                _topicServiceMock.Object,
                _solutionFileRepositoryMock.Object,
                _solutionFileServiceMock.Object,
                _mapperMock.Object);
        }

        [Test]
        public void ShouldInheritFromControllerBase()
        {
            Assert.That(_controller, Is.InstanceOf<ControllerBase>());
        }

        [Test]
        public void GetAssignmentResultsForUser_ShouldReturnAssignmentDetailsIfParametersAreValid()
        {
            //Arrange
            var existingAssignment = new AssignmentBuilder().WithId().Build();
            var userId = _random.NextPositive();

            _assignmentRepositoryMock.Setup(repo => repo.GetSingleWithTestsAndCourseAsync(It.IsAny<int>()))
                .ReturnsAsync(existingAssignment);

            var assignmentResultDto = new AssignmentResultDto();
            _assignmentServiceMock.Setup(service => service.GetResultsForUserAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(assignmentResultDto);

            var returnedTestRunInfo = new AssignmentTestRunInfoDto();
            _assignmentServiceMock
                .Setup(service => service.GetUserTestRunInfoForAssignment(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(returnedTestRunInfo);

            var returnedSolutionFiles = new List<SolutionFile>();
            _solutionFileRepositoryMock
                .Setup(repo => repo.GetAllLatestOfAssignmentForUserAsync(existingAssignment.Id, userId, null))
                .ReturnsAsync(returnedSolutionFiles);

            var returnedModel = new AssignmentDetailModel();
            _assignmentConverterMock
                .Setup(converter =>
                    converter.ToAssignmentDetailModel(It.IsAny<Assignment>(), It.IsAny<AssignmentTestRunInfoDto>(),
                        It.IsAny<IList<TestResult>>(), It.IsAny<IList<SolutionFile>>()))
                .Returns(returnedModel);

            _controller.ControllerContext = new ControllerContextBuilder().WithUser(userId.ToString()).WithRole(Role.Constants.Student).Build();

            //Act
            var actionResult = _controller.GetAssignmentResultsForUser(existingAssignment.Id, userId, null).Result as OkObjectResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            Assert.That(actionResult.Value, Is.EqualTo(returnedModel));
            _assignmentRepositoryMock.Verify(repo => repo.GetSingleWithTestsAndCourseAsync(existingAssignment.Id), Times.Once);
            _assignmentServiceMock.Verify(repo => repo.GetResultsForUserAsync(existingAssignment.Id, userId, null), Times.Once);
            _solutionFileRepositoryMock.Verify();
            _assignmentConverterMock.Verify(converter => converter.ToAssignmentDetailModel(
                existingAssignment, returnedTestRunInfo, assignmentResultDto.TestResults, returnedSolutionFiles), Times.Once);
        }

        [Test]
        public void GetAssignmentResultsForUser_ShouldForbidStudentsToSeeResultsOfOthers()
        {
            //Arrange
            var assignmentId = _random.NextPositive();
            var userId = _random.NextPositive();
            var otherUserId = userId + 1;

            _controller.ControllerContext = new ControllerContextBuilder().WithUser(userId.ToString()).WithRole(Role.Constants.Student).Build();

            //Act
            var actionResult = _controller.GetAssignmentResultsForUser(assignmentId, otherUserId, null).Result as ForbidResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            _assignmentRepositoryMock.Verify(repo => repo.GetSingleWithTestsAndCourseAsync(It.IsAny<int>()), Times.Never);
            _assignmentServiceMock.Verify(service => service.GetResultsForUserAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>()), Times.Never);
            _assignmentConverterMock.Verify(converter => converter.ToAssignmentDetailModel(
                It.IsAny<Assignment>(), It.IsAny<AssignmentTestRunInfoDto>(), It.IsAny<IList<TestResult>>(),
                It.IsAny<IList<SolutionFile>>()), Times.Never);
        }

        [Test]
        public void GetAssignmentResultsForUser_ShouldForbidUsersWithoutRole()
        {
            //Arrange
            var assignmentId = _random.NextPositive();
            var userId = _random.NextPositive();

            _controller.ControllerContext = new ControllerContextBuilder().WithUser(userId.ToString()).Build();

            //Act
            var actionResult = _controller.GetAssignmentResultsForUser(assignmentId, userId, null).Result as ForbidResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            _assignmentRepositoryMock.Verify(repo => repo.GetSingleWithTestsAndCourseAsync(It.IsAny<int>()), Times.Never);
            _assignmentServiceMock.Verify(service => service.GetResultsForUserAsync(It.IsAny<int>(), It.IsAny<int>(), null), Times.Never);
            _assignmentConverterMock.Verify(
                converter => converter.ToAssignmentDetailModel(It.IsAny<Assignment>(),
                    It.IsAny<AssignmentTestRunInfoDto>(), It.IsAny<IList<TestResult>>(),
                    It.IsAny<IList<SolutionFile>>()), Times.Never);
        }

        [Test]
        public void GetAssignmentResultsForTeam_ShouldReturnAssignmentDetailsIfParametersAreValid()
        {
            //Arrange
            var userId = _random.NextPositive();
            var teamId = _random.NextPositive();
            var existingTeam = new ProjectTeamBuilder().WithId().WithUser(userId).Build();
            _projectTeamRepositoryMock.Setup(repo => repo.LoadByIdAsync(It.IsAny<int>())).ReturnsAsync(existingTeam);

            var existingAssignment = new AssignmentBuilder().WithId().Build();
            _assignmentRepositoryMock.Setup(repo => repo.GetSingleWithTestsAndCourseAsync(It.IsAny<int>()))
                .ReturnsAsync(existingAssignment);

            var assignmentResultDto = new AssignmentResultDto();
            _assignmentServiceMock.Setup(service => service.GetResultsForTeamAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(assignmentResultDto);

            var returnedTestRunInfo = new AssignmentTestRunInfoDto();
            _assignmentServiceMock
                .Setup(service => service.GetTeamTestRunInfoForAssignment(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(returnedTestRunInfo);

            var returnedSolutionFiles = new List<SolutionFile>();
            _solutionFileRepositoryMock
                .Setup(repo => repo.GetAllLatestOfAssignmentForTeamAsync(existingAssignment.Id, teamId, null))
                .ReturnsAsync(returnedSolutionFiles);

            var returnedModel = new AssignmentDetailModel();
            _assignmentConverterMock
                .Setup(converter =>
                    converter.ToAssignmentDetailModel(It.IsAny<Assignment>(), It.IsAny<AssignmentTestRunInfoDto>(),
                        It.IsAny<IList<TestResult>>(), It.IsAny<IList<SolutionFile>>()))
                .Returns(returnedModel);

            _controller.ControllerContext = new ControllerContextBuilder().WithUser(userId.ToString()).WithRole(Role.Constants.Student).Build();

            //Act
            var actionResult = _controller.GetAssignmentResultsForTeam(existingAssignment.Id, teamId, null).Result as OkObjectResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            Assert.That(actionResult.Value, Is.EqualTo(returnedModel));
            _projectTeamRepositoryMock.Verify(repo => repo.LoadByIdAsync(teamId), Times.Once);
            _assignmentRepositoryMock.Verify(repo => repo.GetSingleWithTestsAndCourseAsync(existingAssignment.Id), Times.Once);
            _solutionFileRepositoryMock.Verify();
            _assignmentServiceMock.Verify(repo => repo.GetTeamTestRunInfoForAssignment(existingAssignment.Id, teamId, null), Times.Once);
            _assignmentServiceMock.Verify(repo => repo.GetResultsForTeamAsync(existingTeam.ProjectId, existingAssignment.Id, teamId, null), Times.Once);
            _assignmentConverterMock.Verify(
                converter => converter.ToAssignmentDetailModel(existingAssignment, returnedTestRunInfo,
                    assignmentResultDto.TestResults, returnedSolutionFiles), Times.Once);
        }

        [Test]
        public void GetAssignmentResultsForTeam_ShouldForbidStudentsToSeeResultsOfOtherTeams()
        {
            //Arrange
            var assignmentId = _random.NextPositive();
            var userId = _random.NextPositive();
            
            var otherUserId = _random.NextPositive();
            var otherTeam = new ProjectTeamBuilder().WithId().WithUser(otherUserId).Build();
            _projectTeamRepositoryMock.Setup(repo => repo.LoadByIdAsync(It.IsAny<int>())).ReturnsAsync(otherTeam);

            _controller.ControllerContext = new ControllerContextBuilder().WithUser(userId.ToString()).WithRole(Role.Constants.Student).Build();

            //Act
            var actionResult = _controller.GetAssignmentResultsForTeam(assignmentId, otherTeam.Id, null).Result as ForbidResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            _projectTeamRepositoryMock.Verify(repo => repo.LoadByIdAsync(otherTeam.Id), Times.Once);
            _assignmentRepositoryMock.Verify(repo => repo.GetSingleWithTestsAndCourseAsync(It.IsAny<int>()), Times.Never);
            _assignmentServiceMock.Verify(repo => repo.GetTeamTestRunInfoForAssignment(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>()), Times.Never);
            _assignmentServiceMock.Verify(repo => repo.GetResultsForTeamAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>()), Times.Never);
            _assignmentConverterMock.Verify(
                converter => converter.ToAssignmentDetailModel(It.IsAny<Assignment>(),
                    It.IsAny<AssignmentTestRunInfoDto>(), It.IsAny<IList<TestResult>>(),
                    It.IsAny<IList<SolutionFile>>()), Times.Never);
        }

        [Test]
        public void GetAssignmentResultsForTeam_ShouldForbidUsersWithoutRole()
        {
            //Arrange
            var assignmentId = _random.NextPositive();
            var userId = _random.NextPositive();
            var teamId = new ProjectTeamBuilder().WithId().WithUser(userId).Build();
            _projectTeamRepositoryMock.Setup(repo => repo.LoadByIdAsync(It.IsAny<int>())).ReturnsAsync(teamId);

            _controller.ControllerContext = new ControllerContextBuilder().WithUser(userId.ToString()).Build();

            //Act
            var actionResult = _controller.GetAssignmentResultsForTeam(assignmentId, teamId.Id, null).Result as ForbidResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            _projectTeamRepositoryMock.Verify(repo => repo.LoadByIdAsync(teamId.Id), Times.Once);
            _assignmentRepositoryMock.Verify(repo => repo.GetSingleWithTestsAndCourseAsync(It.IsAny<int>()), Times.Never);
            _assignmentServiceMock.Verify(repo => repo.GetTeamTestRunInfoForAssignment(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>()), Times.Never);
            _assignmentServiceMock.Verify(repo => repo.GetResultsForTeamAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>()), Times.Never);
            _assignmentConverterMock.Verify(
                converter => converter.ToAssignmentDetailModel(It.IsAny<Assignment>(),
                    It.IsAny<AssignmentTestRunInfoDto>(), It.IsAny<IList<TestResult>>(),
                    It.IsAny<IList<SolutionFile>>()), Times.Never);
        }

        [Test]
        public async Task Delete_TestExists_ShouldUseRepoToDeleteTest_ShouldReturnOk()
        {
            // Arrange
            Assignment assignmentToDelete = new AssignmentBuilder().WithId().Build();
            _assignmentRepositoryMock.Setup(r => r.GetByIdAsync(assignmentToDelete.Id)).ReturnsAsync(assignmentToDelete);

            // Act
            var result = (await _controller.Delete(assignmentToDelete.Id)) as OkResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            _assignmentRepositoryMock.Verify(r => r.DeleteAsync(assignmentToDelete), Times.Once);
        }

        [Test]
        public async Task Delete_TestDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            int assignmentId = Random.Shared.NextPositive();
            _assignmentRepositoryMock.Setup(r => r.GetByIdAsync(assignmentId)).ReturnsAsync(() => null);

            // Act
            var result = (await _controller.Delete(assignmentId)) as NotFoundResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            _assignmentRepositoryMock.Verify(r => r.GetByIdAsync(assignmentId), Times.Once);
        }
    }
}