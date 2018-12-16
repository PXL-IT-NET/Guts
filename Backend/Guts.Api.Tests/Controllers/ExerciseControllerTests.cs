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
    public class ExerciseControllerTests
    {
        private ExerciseController _controller;
        private Random _random;
        private Mock<IAssignmentService> _assignmentServiceMock;
        private Mock<IExerciseRepository> _exerciseRepositoryMock;
        private Mock<IExerciseConverter> _exerciseConverterMock;

        [SetUp]
        public void Setup()
        {
            _random = new Random();
            _assignmentServiceMock = new Mock<IAssignmentService>();
            _exerciseRepositoryMock = new Mock<IExerciseRepository>();
            _exerciseConverterMock = new Mock<IExerciseConverter>();

            _controller = new ExerciseController(_assignmentServiceMock.Object,
                _exerciseRepositoryMock.Object, 
                _exerciseConverterMock.Object);
        }

        [Test]
        public void ShouldInheritFromControllerBase()
        {
            Assert.That(_controller, Is.InstanceOf<ControllerBase>());
        }

        [Test]
        public void GetExerciseResultsForUserShouldReturnExerciseDetailsIfParamatersAreValid()
        {
            //Arrange
            var existingExercise = new ExerciseBuilder().WithId().Build();
            var userId = _random.NextPositive();

            _exerciseRepositoryMock.Setup(repo => repo.GetSingleWithTestsAndCourseAsync(It.IsAny<int>()))
                .ReturnsAsync(existingExercise);

            var assignmentResultDto = new AssignmentResultDto();
            _assignmentServiceMock.Setup(service => service.GetResultsForUserAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(assignmentResultDto);

            var returnedTestRunInfo = new ExerciseTestRunInfoDto();
            _assignmentServiceMock
                .Setup(service => service.GetUserTestRunInfoForExercise(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(returnedTestRunInfo);

            var returnedModel = new ExerciseDetailModel();
            _exerciseConverterMock
                .Setup(converter =>
                    converter.ToExerciseDetailModel(It.IsAny<Exercise>(), It.IsAny<IList<TestResult>>(), It.IsAny<ExerciseTestRunInfoDto>()))
                .Returns(returnedModel);

            _controller.ControllerContext = new ControllerContextBuilder().WithUser(userId.ToString()).WithRole(Role.Constants.Student).Build();

            //Act
            var actionResult = _controller.GetExerciseResultsForUser(existingExercise.Id, userId, null).Result as OkObjectResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            Assert.That(actionResult.Value, Is.EqualTo(returnedModel));
            _exerciseRepositoryMock.Verify(repo => repo.GetSingleWithTestsAndCourseAsync(existingExercise.Id), Times.Once);
            _assignmentServiceMock.Verify(repo => repo.GetResultsForUserAsync(existingExercise.Id, userId, null), Times.Once);
            _exerciseConverterMock.Verify(converter => converter.ToExerciseDetailModel(existingExercise, assignmentResultDto.TestResults, returnedTestRunInfo), Times.Once);
        }

        [Test]
        public void GetExerciseResultsForUserShouldForbidStudentsToSeeResultsOfOthers()
        {
            //Arrange
            var exerciseId = _random.NextPositive();
            var userId = _random.NextPositive();
            var otherUserId = userId + 1;

            _controller.ControllerContext = new ControllerContextBuilder().WithUser(userId.ToString()).WithRole(Role.Constants.Student).Build();

            //Act
            var actionResult = _controller.GetExerciseResultsForUser(exerciseId, otherUserId, null).Result as ForbidResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            _exerciseRepositoryMock.Verify(repo => repo.GetSingleWithTestsAndCourseAsync(It.IsAny<int>()), Times.Never);
            _assignmentServiceMock.Verify(service => service.GetResultsForUserAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>()), Times.Never);
            _exerciseConverterMock.Verify(converter => converter.ToExerciseDetailModel(It.IsAny<Exercise>(), It.IsAny<IList<TestResult>>(), It.IsAny<ExerciseTestRunInfoDto>()), Times.Never);
        }

        [Test]
        public void GetExerciseResultsForUserShouldForbidUsersWithoutRole()
        {
            //Arrange
            var exerciseId = _random.NextPositive();
            var userId = _random.NextPositive();

            _controller.ControllerContext = new ControllerContextBuilder().WithUser(userId.ToString()).Build();

            //Act
            var actionResult = _controller.GetExerciseResultsForUser(exerciseId, userId, null).Result as ForbidResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            _exerciseRepositoryMock.Verify(repo => repo.GetSingleWithTestsAndCourseAsync(It.IsAny<int>()), Times.Never);
            _assignmentServiceMock.Verify(service => service.GetResultsForUserAsync(It.IsAny<int>(), It.IsAny<int>(), null), Times.Never);
            _exerciseConverterMock.Verify(converter => converter.ToExerciseDetailModel(It.IsAny<Exercise>(), It.IsAny<IList<TestResult>>(), It.IsAny<ExerciseTestRunInfoDto>()), Times.Never);
        }

        //[Test]
        //public void GetExerciseStudentsShouldReturnUsersWithStudentRoleThatHaveTestResultsForTheExercise()
        //{
        //    //Arrange
        //    var exerciseId = _random.NextPositive();
        //    var userId = _random.NextPositive();
        //    var usersForExercise = new List<User>
        //    {
        //        new User()
        //    };
        //    var convertedUser = new UserModel();

        //    _controller.ControllerContext = new ControllerContextBuilder().WithUser(userId.ToString()).WithRole(Role.Constants.Lector).Build();
        //    _exerciseRepositoryMock.Setup(repo => repo.GetExerciseUsersAsync(It.IsAny<int>()))
        //        .ReturnsAsync(usersForExercise);
        //    _userConverterMock.Setup(converter => converter.FromUser(It.IsAny<User>())).Returns(convertedUser);


        //    //Act
        //    var okObjectResult = _controller.GetExerciseStudents(exerciseId).Result as OkObjectResult;

        //    //Assert
        //    Assert.That(okObjectResult, Is.Not.Null);
        //    Assert.That(okObjectResult.Value, Is.EquivalentTo(new List<UserModel>{convertedUser}));
            
        //    _exerciseRepositoryMock.Verify(repo => repo.GetExerciseUsersAsync(exerciseId), Times.Once);
        //    _userConverterMock.Verify(converter => converter.FromUser(It.IsIn<User>(usersForExercise)), Times.Exactly(usersForExercise.Count));
        //}
    }
}