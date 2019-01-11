using System;
using System.Collections.Generic;
using Guts.Api.Controllers;
using Guts.Api.Models;
using Guts.Api.Models.Converters;
using Guts.Api.Tests.Builders;
using Guts.Business;
using Guts.Business.Services;
using Guts.Business.Tests.Builders;
using Guts.Common.Extensions;
using Guts.Data.Repositories;
using Guts.Domain;
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

        [SetUp]
        public void Setup()
        {
            _random = new Random();
            _assignmentServiceMock = new Mock<IAssignmentService>();
            _assignmentRepositoryMock = new Mock<IAssignmentRepository>();
            _assignmentConverterMock = new Mock<IAssignmentConverter>();

            _controller = new AssignmentController(_assignmentServiceMock.Object,
                _assignmentRepositoryMock.Object, 
                _assignmentConverterMock.Object);
        }

        [Test]
        public void ShouldInheritFromControllerBase()
        {
            Assert.That(_controller, Is.InstanceOf<ControllerBase>());
        }

        [Test]
        public void GetAssignmentResultsForUser_ShouldReturnAssignmentDetailsIfParamatersAreValid()
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

            var returnedModel = new AssignmentDetailModel();
            _assignmentConverterMock
                .Setup(converter =>
                    converter.ToAssignmentDetailModel(It.IsAny<Assignment>(), It.IsAny<IList<TestResult>>(), It.IsAny<AssignmentTestRunInfoDto>()))
                .Returns(returnedModel);

            _controller.ControllerContext = new ControllerContextBuilder().WithUser(userId.ToString()).WithRole(Role.Constants.Student).Build();

            //Act
            var actionResult = _controller.GetAssignmentResultsForUser(existingAssignment.Id, userId, null).Result as OkObjectResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            Assert.That(actionResult.Value, Is.EqualTo(returnedModel));
            _assignmentRepositoryMock.Verify(repo => repo.GetSingleWithTestsAndCourseAsync(existingAssignment.Id), Times.Once);
            _assignmentServiceMock.Verify(repo => repo.GetResultsForUserAsync(existingAssignment.Id, userId, null), Times.Once);
            _assignmentConverterMock.Verify(converter => converter.ToAssignmentDetailModel(existingAssignment, assignmentResultDto.TestResults, returnedTestRunInfo), Times.Once);
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
            _assignmentConverterMock.Verify(converter => converter.ToAssignmentDetailModel(It.IsAny<Assignment>(), It.IsAny<IList<TestResult>>(), It.IsAny<AssignmentTestRunInfoDto>()), Times.Never);
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
            _assignmentConverterMock.Verify(converter => converter.ToAssignmentDetailModel(It.IsAny<Assignment>(), It.IsAny<IList<TestResult>>(), It.IsAny<AssignmentTestRunInfoDto>()), Times.Never);
        }
    }
}