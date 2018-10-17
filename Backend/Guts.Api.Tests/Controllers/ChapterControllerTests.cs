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
using Guts.Data;
using Guts.Data.Repositories;
using Guts.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ControllerBase = Guts.Api.Controllers.ControllerBase;

namespace Guts.Api.Tests.Controllers
{
    [TestFixture]
    public class ChapterControllerTests
    {
        private Mock<IChapterService> _chapterServiceMock;
        private Mock<IChapterConverter> _chapterConverterMock;
        private ChapterController _controller;
        private Random _random;
        private int _userId;
        private Mock<UserManager<User>> _userManagerMock;
        private Mock<IChapterRepository> _chapterRepositoryMock;

        [SetUp]
        public void Setup()
        {
            _random = new Random();
            _userId = _random.NextPositive();
            _chapterServiceMock = new Mock<IChapterService>();
            _chapterRepositoryMock = new Mock<IChapterRepository>();
            _chapterConverterMock = new Mock<IChapterConverter>();

            var userStoreMock = new Mock<IUserStore<User>>();
            var passwordHasherMock = new Mock<IPasswordHasher<User>>();
            var lookupNormalizerMock = new Mock<ILookupNormalizer>();
            var errorsMock = new Mock<IdentityErrorDescriber>();
            var loggerMock = new Mock<ILogger<UserManager<User>>>();

            _userManagerMock = new Mock<UserManager<User>>(
                userStoreMock.Object,
                null,
                passwordHasherMock.Object,
                null,
                null,
                lookupNormalizerMock.Object,
                errorsMock.Object,
                null,
                loggerMock.Object);

            _controller = new ChapterController(_chapterServiceMock.Object, 
                _chapterRepositoryMock.Object, 
                _chapterConverterMock.Object, 
                _userManagerMock.Object)
            {
                ControllerContext = new ControllerContextBuilder().WithUser(_userId.ToString()).WithRole(Role.Constants.Student).Build()
            };
           
        }

        [Test]
        public void ShouldInheritFromControllerBase()
        {
            Assert.That(_controller, Is.InstanceOf<ControllerBase>());
        }

        [Test]
        public void GetChapterSummaryShouldReturnContentsIfParamatersAreValid()
        {
            //Arrange
            var existingChapter = new ChapterBuilder().WithId().Build();
            var chapterContents = new ChapterSummaryModel();
            var userExerciseResults = new List<ExerciseResultDto>();
            var averageExerciseResults = new List<ExerciseResultDto>();
            _chapterServiceMock.Setup(service => service.LoadChapterWithTestsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(existingChapter);
            _chapterServiceMock.Setup(service => service.GetResultsForUserAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(userExerciseResults);
            _chapterServiceMock.Setup(service => service.GetAverageResultsAsync(It.IsAny<int>()))
                .ReturnsAsync(averageExerciseResults);
            _chapterConverterMock.Setup(converter => converter.ToChapterSummaryModel(It.IsAny<Chapter>(), 
                It.IsAny<IList<ExerciseResultDto>>(), 
                It.IsAny<IList<ExerciseResultDto>>()))
                .Returns(chapterContents);

            //Act
            var actionResult = _controller.GetChapterSummary(existingChapter.CourseId, existingChapter.Number, _userId, null).Result as OkObjectResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            _chapterServiceMock.Verify(service => service.LoadChapterWithTestsAsync(existingChapter.CourseId, existingChapter.Number), Times.Once);

            _chapterServiceMock.Verify(service => service.GetResultsForUserAsync(existingChapter.Id, _userId, null), Times.Once);
            _chapterServiceMock.Verify(service => service.GetAverageResultsAsync(existingChapter.Id), Times.Once);

            _chapterConverterMock.Verify(converter => converter.ToChapterSummaryModel(existingChapter, userExerciseResults, averageExerciseResults), Times.Once);
            Assert.That(actionResult.Value, Is.EqualTo(chapterContents));
        }

        [Test]
        [TestCase(0, 1)]
        [TestCase(1, 0)]
        [TestCase(-1, 1)]
        [TestCase(1, -1)]
        public void GetChapterSummaryShouldReturnBadRequestOnInvalidInput(int courseId, int chapterNumber)
        {
            //Act
            var actionResult = _controller.GetChapterSummary(courseId, chapterNumber, 0, null).Result as BadRequestResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            _chapterServiceMock.Verify(service => service.LoadChapterWithTestsAsync(courseId, chapterNumber), Times.Never);
        }

        [Test]
        public void GetChapterSummaryShouldReturnNotFoundWhenServiceCannotFindChapter()
        {
            //Arrange
            _chapterServiceMock.Setup(service => service.LoadChapterWithTestsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .Throws<DataNotFoundException>();

            var courseId = _random.NextPositive();
            var chapter = _random.NextPositive();

            //Act
            var actionResult = _controller.GetChapterSummary(courseId, chapter, _userId, null).Result as NotFoundResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            _chapterServiceMock.Verify(service => service.LoadChapterWithTestsAsync(courseId, chapter), Times.Once);
        }
    }
}
