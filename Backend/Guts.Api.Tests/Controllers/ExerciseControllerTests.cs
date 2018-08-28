using System;
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
        private Mock<IExerciseService> _exerciseServiceMock;
        private Mock<IExerciseRepository> _exerciseRepositoryMock;
        private Mock<IExerciseConverter> _exerciseConverterMock;

        [SetUp]
        public void Setup()
        {
            _random = new Random();
            _exerciseServiceMock = new Mock<IExerciseService>();
            _exerciseRepositoryMock = new Mock<IExerciseRepository>();
            _exerciseConverterMock = new Mock<IExerciseConverter>();
            
            _controller = new ExerciseController(_exerciseServiceMock.Object, 
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
            var existingExercise = new ExerciseBuilder().Build();
            var userId = _random.NextPositive();

            _exerciseRepositoryMock.Setup(repo => repo.GetSingleWithChapterAndCourseAsync(It.IsAny<int>()))
                .ReturnsAsync(existingExercise);

            var returnedExerciseResultDto = new ExerciseResultDto();
            _exerciseServiceMock.Setup(service => service.GetResultsForUserAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(returnedExerciseResultDto);

            var returnedTestRunInfo = new ExerciseTestRunInfoDto();
            _exerciseServiceMock
                .Setup(service => service.GetUserTestRunInfoForExercise(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(returnedTestRunInfo);

            var returnedModel = new ExerciseDetailModel();
            _exerciseConverterMock
                .Setup(converter =>
                    converter.ToExerciseDetailModel(It.IsAny<Exercise>(), It.IsAny<ExerciseResultDto>(), It.IsAny<ExerciseTestRunInfoDto>()))
                .Returns(returnedModel);

            _controller.ControllerContext = new ControllerContextBuilder().WithUser(userId.ToString()).WithRole(Role.Constants.Student).Build();

            //Act
            var actionResult = _controller.GetExerciseResultsForUser(existingExercise.Id, userId).Result as OkObjectResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            Assert.That(actionResult.Value, Is.EqualTo(returnedModel));
            _exerciseRepositoryMock.Verify(repo => repo.GetSingleWithChapterAndCourseAsync(existingExercise.Id), Times.Once);
            _exerciseServiceMock.Verify(service => service.GetResultsForUserAsync(existingExercise.Id, userId), Times.Once);
            _exerciseConverterMock.Verify(converter => converter.ToExerciseDetailModel(existingExercise, returnedExerciseResultDto, returnedTestRunInfo), Times.Once);
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
            var actionResult = _controller.GetExerciseResultsForUser(exerciseId, otherUserId).Result as ForbidResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            _exerciseRepositoryMock.Verify(repo => repo.GetSingleWithChapterAndCourseAsync(It.IsAny<int>()), Times.Never);
            _exerciseServiceMock.Verify(service => service.GetResultsForUserAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
            _exerciseConverterMock.Verify(converter => converter.ToExerciseDetailModel(It.IsAny<Exercise>(), It.IsAny<ExerciseResultDto>(), It.IsAny<ExerciseTestRunInfoDto>()), Times.Never);
        }

        [Test]
        public void GetExerciseResultsForUserShouldForbidUsersWithoutRole()
        {
            //Arrange
            var exerciseId = _random.NextPositive();
            var userId = _random.NextPositive();

            _controller.ControllerContext = new ControllerContextBuilder().WithUser(userId.ToString()).Build();

            //Act
            var actionResult = _controller.GetExerciseResultsForUser(exerciseId, userId).Result as ForbidResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            _exerciseRepositoryMock.Verify(repo => repo.GetSingleWithChapterAndCourseAsync(It.IsAny<int>()), Times.Never);
            _exerciseServiceMock.Verify(service => service.GetResultsForUserAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
            _exerciseConverterMock.Verify(converter => converter.ToExerciseDetailModel(It.IsAny<Exercise>(), It.IsAny<ExerciseResultDto>(), It.IsAny<ExerciseTestRunInfoDto>()), Times.Never);
        }
    }
}