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
using Microsoft.Extensions.Caching.Memory;
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
        private Mock<IMemoryCache> _memoryCacheMock;
        private Mock<IUserRepository> _userRepositoryMock;

        [SetUp]
        public void Setup()
        {
            _random = new Random();
            _userId = _random.NextPositive();
            _chapterServiceMock = new Mock<IChapterService>();
            _chapterRepositoryMock = new Mock<IChapterRepository>();
            _chapterConverterMock = new Mock<IChapterConverter>();
            _memoryCacheMock = new Mock<IMemoryCache>();
            _userRepositoryMock = new Mock<IUserRepository>();

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

            object cachedChapterStatisticsModel;
            _memoryCacheMock.Setup(cache => cache.TryGetValue(It.IsAny<object>(), out cachedChapterStatisticsModel)).Returns(false);

            _controller = new ChapterController(_chapterServiceMock.Object, 
                _chapterRepositoryMock.Object, 
                _chapterConverterMock.Object, 
                _userManagerMock.Object,
                _userRepositoryMock.Object,
                _memoryCacheMock.Object)
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
            var userExerciseResults = new List<AssignmentResultDto>();
          
            _chapterServiceMock.Setup(service => service.LoadChapterWithTestsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(existingChapter);
            _chapterServiceMock.Setup(service => service.GetResultsForUserAsync(It.IsAny<Chapter>(), It.IsAny<int>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(userExerciseResults);
            _chapterConverterMock.Setup(converter => converter.ToChapterSummaryModel(It.IsAny<Chapter>(), 
                It.IsAny<IList<AssignmentResultDto>>()))
                .Returns(chapterContents);

            //Act
            var actionResult = _controller.GetChapterSummary(existingChapter.CourseId, existingChapter.Number, _userId, null).Result as OkObjectResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            _chapterServiceMock.Verify(service => service.LoadChapterWithTestsAsync(existingChapter.CourseId, existingChapter.Number), Times.Once);

            _chapterServiceMock.Verify(service => service.GetResultsForUserAsync(existingChapter, _userId, null), Times.Once);

            _chapterConverterMock.Verify(converter => converter.ToChapterSummaryModel(existingChapter, userExerciseResults), Times.Once);
            Assert.That(actionResult.Value, Is.EqualTo(chapterContents));
        }

        [Test]
        [TestCase(0, 1, 1)]
        [TestCase(1, 0, 1)]
        [TestCase(-1, 1, 1)]
        [TestCase(1, -1, 1)]
        [TestCase(1, 1, 0)]
        public void GetChapterSummaryShouldReturnBadRequestOnInvalidInput(int courseId, int chapterNumber, int userId)
        {
            //Act
            var actionResult = _controller.GetChapterSummary(courseId, chapterNumber, userId, null).Result as BadRequestResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            _chapterServiceMock.Verify(service => service.LoadChapterAsync(courseId, chapterNumber), Times.Never);
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

        [Test]
        [TestCase(0, 1)]
        [TestCase(1, 0)]
        [TestCase(-1, 1)]
        [TestCase(1, -1)]
        public void GetChapterStatisticsShouldReturnBadRequestOnInvalidInput(int courseId, int chapterNumber)
        {
            //Act
            var actionResult = _controller.GetChapterStatistics(courseId, chapterNumber, null).Result as BadRequestResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            _chapterServiceMock.Verify(service => service.LoadChapterAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Test]
        public void GetChapterStatisticsShouldReturnNotFoundWhenServiceCannotFindChapter()
        {
            //Arrange
            _chapterServiceMock.Setup(service => service.LoadChapterAsync(It.IsAny<int>(), It.IsAny<int>()))
                .Throws<DataNotFoundException>();

            var courseId = _random.NextPositive();
            var chapter = _random.NextPositive();

            //Act
            var actionResult = _controller.GetChapterStatistics(courseId, chapter, null).Result as NotFoundResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            _chapterServiceMock.Verify(service => service.LoadChapterAsync(courseId, chapter), Times.Once);
        }

        [Test]
        public void GetChapterStatisticsShouldReturnStatisticsIfParamatersAreValid()
        {
            //Arrange
            var existingChapter = new ChapterBuilder().WithId().Build();
            var chapterStatistics = new List<AssignmentStatisticsDto>();
            var chapterStatisticsModel = new ChapterStatisticsModel();
            var date = DateTime.Now.AddDays(-1);
           

            _chapterServiceMock.Setup(service => service.LoadChapterAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(existingChapter);

            _chapterServiceMock.Setup(service => service.GetChapterStatisticsAsync(It.IsAny<Chapter>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(chapterStatistics);

            _chapterConverterMock.Setup(converter => converter.ToChapterStatisticsModel(It.IsAny<Chapter>(), It.IsAny<IList<AssignmentStatisticsDto>>()))
                .Returns(chapterStatisticsModel);

            //Act
            var actionResult = _controller.GetChapterStatistics(existingChapter.CourseId, existingChapter.Number, date).Result as OkObjectResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            _chapterServiceMock.Verify(service => service.LoadChapterAsync(existingChapter.CourseId, existingChapter.Number), Times.Once);
            _chapterServiceMock.Verify(service => service.GetChapterStatisticsAsync(existingChapter, date.ToUniversalTime()), Times.Once);
            _chapterConverterMock.Verify(converter => converter.ToChapterStatisticsModel(existingChapter, chapterStatistics), Times.Once);
            Assert.That(actionResult.Value, Is.EqualTo(chapterStatisticsModel));
        }

        [Test]
        public void GetChapterStatisticsShouldUseCacheWhenDateIsCloseToNow()
        {
            //Arrange
            var courseId = _random.NextPositive();
            var chapterNumber = _random.NextPositive();
            var date = DateTime.Now.AddSeconds(-ChapterController.CacheTimeInSeconds + 1);
            var expectedCacheKey = $"GetChapterStatistics-{courseId}-{chapterNumber}";

            object cachedChapterStatisticsModel = new ChapterStatisticsModel();
            _memoryCacheMock.Setup(cache => cache.TryGetValue(It.IsAny<object>(), out cachedChapterStatisticsModel))
                .Returns(true);
          
            //Act
            var actionResult = _controller.GetChapterStatistics(courseId, chapterNumber, date).Result as OkObjectResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            Assert.That(actionResult.Value, Is.EqualTo(cachedChapterStatisticsModel));

            _memoryCacheMock.Verify(cache => cache.TryGetValue(expectedCacheKey, out cachedChapterStatisticsModel), Times.Once);
            _chapterServiceMock.Verify(service => service.LoadChapterAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Test]
        public void GetChapterStatisticsShouldNotUseCacheWhenDateIsTooFarInThePast()
        {
            //Arrange
            var existingChapter = new ChapterBuilder().WithId().Build();
            var chapterStatistics = new List<AssignmentStatisticsDto>();
            var chapterStatisticsModel = new ChapterStatisticsModel();
            var aLongTimeAgo = DateTime.Now.AddSeconds(-ChapterController.CacheTimeInSeconds - 1);


            _chapterServiceMock.Setup(service => service.LoadChapterAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(existingChapter);

            _chapterServiceMock.Setup(service => service.GetChapterStatisticsAsync(It.IsAny<Chapter>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(chapterStatistics);

            _chapterConverterMock.Setup(converter => converter.ToChapterStatisticsModel(It.IsAny<Chapter>(), It.IsAny<IList<AssignmentStatisticsDto>>()))
                .Returns(chapterStatisticsModel);

            //Act
            var actionResult = _controller.GetChapterStatistics(existingChapter.CourseId, existingChapter.Number, aLongTimeAgo).Result as OkObjectResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            object cachedEntry;
            _memoryCacheMock.Verify(cache => cache.TryGetValue(It.IsAny<object>(), out cachedEntry), Times.Never);

            _chapterServiceMock.Verify(service => service.LoadChapterAsync(existingChapter.CourseId, existingChapter.Number), Times.Once);
            _chapterServiceMock.Verify(service => service.GetChapterStatisticsAsync(existingChapter, aLongTimeAgo.ToUniversalTime()), Times.Once);
            _chapterConverterMock.Verify(converter => converter.ToChapterStatisticsModel(existingChapter, chapterStatistics), Times.Once);
            Assert.That(actionResult.Value, Is.EqualTo(chapterStatisticsModel));
        }

        [Test]
        public void GetChapterStatisticsShouldCacheCreatedModel()
        {
            //Arrange
            var existingChapter = new ChapterBuilder().WithId().Build();
            var chapterStatistics = new List<AssignmentStatisticsDto>();
            var chapterStatisticsModel = new ChapterStatisticsModel();
            var date = DateTime.Now.AddSeconds(-ChapterController.CacheTimeInSeconds + 1);
            var expectedCacheKey = $"GetChapterStatistics-{existingChapter.CourseId}-{existingChapter.Number}";


            _chapterServiceMock.Setup(service => service.LoadChapterAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(existingChapter);

            _chapterServiceMock.Setup(service => service.GetChapterStatisticsAsync(It.IsAny<Chapter>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(chapterStatistics);

            _chapterConverterMock.Setup(converter => converter.ToChapterStatisticsModel(It.IsAny<Chapter>(), It.IsAny<IList<AssignmentStatisticsDto>>()))
                .Returns(chapterStatisticsModel);

            object cachedChapterStatisticsModel;
            _memoryCacheMock.Setup(cache => cache.TryGetValue(It.IsAny<object>(), out cachedChapterStatisticsModel))
                .Returns(false);

            var cacheEntryMock = new Mock<ICacheEntry>();
            _memoryCacheMock.Setup(cache => cache.CreateEntry(It.IsAny<object>())).Returns(cacheEntryMock.Object);

            //Act
            var actionResult = _controller.GetChapterStatistics(existingChapter.CourseId, existingChapter.Number, date).Result as OkObjectResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            _memoryCacheMock.Verify(cache => cache.TryGetValue(expectedCacheKey, out cachedChapterStatisticsModel), Times.Once);
            _memoryCacheMock.Verify(cache => cache.CreateEntry(expectedCacheKey), Times.Once);

            Assert.That(actionResult.Value, Is.EqualTo(chapterStatisticsModel));
        }
    }
}
