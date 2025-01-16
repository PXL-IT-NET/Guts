using System;
using System.Collections.Generic;
using Guts.Api.Controllers;
using Guts.Api.Models;
using Guts.Api.Models.Converters;
using Guts.Api.Tests.Builders;
using Guts.Business;
using Guts.Business.Dtos;
using Guts.Business.Repositories;
using Guts.Business.Services;
using Guts.Business.Tests.Builders;
using Guts.Common.Extensions;
using Guts.Domain.RoleAggregate;
using Guts.Domain.TopicAggregate.ChapterAggregate;
using Guts.Domain.UserAggregate;
using Guts.Domain.ValueObjects;
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
        private Mock<ITopicConverter> _topicConverterMock;
        private ChapterController _controller;
        private int _userId;
        private Mock<UserManager<User>> _userManagerMock;
        private Mock<IMemoryCache> _memoryCacheMock;
        private Mock<IUserRepository> _userRepositoryMock;

        [SetUp]
        public void Setup()
        {
            _userId = Random.Shared.NextPositive();
            _chapterServiceMock = new Mock<IChapterService>();
            _topicConverterMock = new Mock<ITopicConverter>();
            _chapterConverterMock = new Mock<IChapterConverter>();
            _memoryCacheMock = new Mock<IMemoryCache>();
            _userRepositoryMock = new Mock<IUserRepository>();

            Mock<IUserStore<User>> userStoreMock = new Mock<IUserStore<User>>();
            Mock<IPasswordHasher<User>> passwordHasherMock = new Mock<IPasswordHasher<User>>();
            Mock<ILookupNormalizer> lookupNormalizerMock = new Mock<ILookupNormalizer>();
            Mock<IdentityErrorDescriber> errorsMock = new Mock<IdentityErrorDescriber>();
            Mock<ILogger<UserManager<User>>> loggerMock = new Mock<ILogger<UserManager<User>>>();

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
                _chapterConverterMock.Object, 
                _topicConverterMock.Object,
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
        public void GetChapterSummaryShouldReturnContentsIfParametersAreValid()
        {
            //Arrange
            Chapter existingChapter = new ChapterBuilder().WithId().Build();
            TopicSummaryModel chapterContents = new TopicSummaryModel();
            var userAssignmentResults = new List<AssignmentResultDto>();
            int periodId = Random.Shared.NextPositive();
          
            _chapterServiceMock.Setup(service => service.LoadChapterWithTestsAsync(It.IsAny<int>(), It.IsAny<Code>(), It.IsAny<int?>()))
                .ReturnsAsync(existingChapter);
            _chapterServiceMock.Setup(service => service.GetResultsForUserAsync(It.IsAny<Chapter>(), It.IsAny<int>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(userAssignmentResults);
            _topicConverterMock.Setup(converter => converter.ToTopicSummaryModel(It.IsAny<Chapter>(), 
                It.IsAny<IReadOnlyList<AssignmentResultDto>>()))
                .Returns(chapterContents);

            //Act
            OkObjectResult actionResult =
                _controller.GetChapterSummary(existingChapter.CourseId, existingChapter.Code, _userId, null, periodId)
                    .Result as OkObjectResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            _chapterServiceMock.Verify(service => service.LoadChapterWithTestsAsync(existingChapter.CourseId, existingChapter.Code, periodId), Times.Once);

            _chapterServiceMock.Verify(service => service.GetResultsForUserAsync(existingChapter, _userId, null), Times.Once);

            _topicConverterMock.Verify(converter => converter.ToTopicSummaryModel(existingChapter, userAssignmentResults), Times.Once);
            Assert.That(actionResult.Value, Is.EqualTo(chapterContents));
        }

        [Test]
        [TestCase(0, "validCode", 1)]
        [TestCase(-1, "validCode", 1)]
        [TestCase(1, "validCode", 0)]
        public void GetChapterSummaryShouldReturnBadRequestOnInvalidInput(int courseId, string chapterCode, int userId)
        {
            //Act
            BadRequestResult actionResult = _controller.GetChapterSummary(courseId, chapterCode, userId, null, null).Result as BadRequestResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            _chapterServiceMock.Verify(service => service.LoadChapterAsync(courseId, chapterCode, null), Times.Never);
        }

        [Test]
        public void GetChapterSummaryShouldReturnNotFoundWhenServiceCannotFindChapter()
        {
            //Arrange
            _chapterServiceMock.Setup(service => service.LoadChapterWithTestsAsync(It.IsAny<int>(), It.IsAny<Code>(), It.IsAny<int?>()))
                .Throws<DataNotFoundException>();

            int courseId = Random.Shared.NextPositive();
            string chapterCode = Guid.NewGuid().ToString();

            //Act
            NotFoundResult actionResult = _controller.GetChapterSummary(courseId, chapterCode, _userId, null).Result as NotFoundResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            _chapterServiceMock.Verify(service => service.LoadChapterWithTestsAsync(courseId, chapterCode, null), Times.Once);
        }

        [Test]
        [TestCase(0, "validCode")]
        [TestCase(1, null)]
        [TestCase(-1, "validCode")]
        [TestCase(1, "")]
        public void GetChapterStatisticsShouldReturnBadRequestOnInvalidInput(int courseId, string chapterCode)
        {
            //Act
            BadRequestResult actionResult = _controller.GetChapterStatistics(courseId, chapterCode, null).Result as BadRequestResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            _chapterServiceMock.Verify(service => service.LoadChapterAsync(It.IsAny<int>(), It.IsAny<Code>(), It.IsAny<int?>()), Times.Never);
        }

        [Test]
        public void GetChapterStatisticsShouldReturnNotFoundWhenServiceCannotFindChapter()
        {
            //Arrange
            _chapterServiceMock.Setup(service => service.LoadChapterAsync(It.IsAny<int>(), It.IsAny<Code>(), It.IsAny<int?>()))
                .Throws<DataNotFoundException>();

            int courseId = Random.Shared.NextPositive();
            string chapterCode = Guid.NewGuid().ToString();
            int periodId = Random.Shared.NextPositive();

            //Act
            NotFoundResult actionResult = _controller.GetChapterStatistics(courseId, chapterCode, null, periodId).Result as NotFoundResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            _chapterServiceMock.Verify(service => service.LoadChapterAsync(courseId, chapterCode, periodId), Times.Once);
        }

        [Test]
        public void GetChapterStatisticsShouldReturnStatisticsIfParametersAreValid()
        {
            //Arrange
            Chapter existingChapter = new ChapterBuilder().WithId().Build();
            var chapterStatistics = new List<AssignmentStatisticsDto>();
            TopicStatisticsModel chapterStatisticsModel = new TopicStatisticsModel();
            DateTime date = DateTime.Now.AddDays(-1);
            int periodId = Random.Shared.NextPositive();


            _chapterServiceMock.Setup(service => service.LoadChapterAsync(It.IsAny<int>(), It.IsAny<Code>(), It.IsAny<int?>()))
                .ReturnsAsync(existingChapter);

            _chapterServiceMock.Setup(service => service.GetChapterStatisticsAsync(It.IsAny<Chapter>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(chapterStatistics);

            _topicConverterMock.Setup(converter => converter.ToTopicStatisticsModel(It.IsAny<Chapter>(), It.IsAny<IReadOnlyList<AssignmentStatisticsDto>>(), It.IsAny<string>()))
                .Returns(chapterStatisticsModel);

            //Act
            OkObjectResult actionResult =
                _controller.GetChapterStatistics(existingChapter.CourseId, existingChapter.Code, date, periodId)
                    .Result as OkObjectResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            _chapterServiceMock.Verify(service => service.LoadChapterAsync(existingChapter.CourseId, existingChapter.Code, periodId), Times.Once);
            _chapterServiceMock.Verify(service => service.GetChapterStatisticsAsync(existingChapter, date.ToUniversalTime()), Times.Once);
            _topicConverterMock.Verify(converter => converter.ToTopicStatisticsModel(existingChapter, chapterStatistics, "Students"), Times.Once);
            Assert.That(actionResult.Value, Is.EqualTo(chapterStatisticsModel));
        }

        [Test]
        public void GetChapterStatisticsShouldUseCacheWhenDateIsCloseToNow()
        {
            //Arrange
            int courseId = Random.Shared.NextPositive();
            string chapterCode = Guid.NewGuid().ToString();
            DateTime date = DateTime.Now.AddSeconds(-ChapterController.CacheTimeInSeconds + 1);
            string expectedCacheKey = $"GetChapterStatistics-{courseId}-{chapterCode}-0";

            object cachedChapterStatisticsModel = new TopicStatisticsModel();
            _memoryCacheMock.Setup(cache => cache.TryGetValue(It.IsAny<object>(), out cachedChapterStatisticsModel))
                .Returns(true);
          
            //Act
            OkObjectResult actionResult = _controller.GetChapterStatistics(courseId, chapterCode, date, null).Result as OkObjectResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            Assert.That(actionResult.Value, Is.EqualTo(cachedChapterStatisticsModel));

            _memoryCacheMock.Verify(cache => cache.TryGetValue(expectedCacheKey, out cachedChapterStatisticsModel), Times.Once);
            _chapterServiceMock.Verify(service => service.LoadChapterAsync(It.IsAny<int>(), It.IsAny<Code>(), It.IsAny<int?>()), Times.Never);
        }

        [Test]
        public void GetChapterStatisticsShouldNotUseCacheWhenDateIsTooFarInThePast()
        {
            //Arrange
            Chapter existingChapter = new ChapterBuilder().WithId().Build();
            List<AssignmentStatisticsDto> chapterStatistics = new List<AssignmentStatisticsDto>();
            TopicStatisticsModel chapterStatisticsModel = new TopicStatisticsModel();
            DateTime aLongTimeAgo = DateTime.Now.AddSeconds(-ChapterController.CacheTimeInSeconds - 1);


            _chapterServiceMock.Setup(service => service.LoadChapterAsync(It.IsAny<int>(), It.IsAny<Code>(), It.IsAny<int?>()))
                .ReturnsAsync(existingChapter);

            _chapterServiceMock.Setup(service => service.GetChapterStatisticsAsync(It.IsAny<Chapter>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(chapterStatistics);

            _topicConverterMock.Setup(converter => converter.ToTopicStatisticsModel(It.IsAny<Chapter>(), It.IsAny<IReadOnlyList<AssignmentStatisticsDto>>(), It.IsAny<string>()))
                .Returns(chapterStatisticsModel);

            //Act
            OkObjectResult actionResult = _controller.GetChapterStatistics(existingChapter.CourseId, existingChapter.Code, aLongTimeAgo, null).Result as OkObjectResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            object cachedEntry;
            _memoryCacheMock.Verify(cache => cache.TryGetValue(It.IsAny<object>(), out cachedEntry), Times.Never);

            _chapterServiceMock.Verify(service => service.LoadChapterAsync(existingChapter.CourseId, existingChapter.Code, null), Times.Once);
            _chapterServiceMock.Verify(service => service.GetChapterStatisticsAsync(existingChapter, aLongTimeAgo.ToUniversalTime()), Times.Once);
            _topicConverterMock.Verify(converter => converter.ToTopicStatisticsModel(existingChapter, chapterStatistics, "Students"), Times.Once);
            Assert.That(actionResult.Value, Is.EqualTo(chapterStatisticsModel));
        }

        [Test]
        public void GetChapterStatisticsShouldCacheCreatedModel()
        {
            //Arrange
            Chapter existingChapter = new ChapterBuilder().WithId().Build();
            List<AssignmentStatisticsDto> chapterStatistics = new List<AssignmentStatisticsDto>();
            TopicStatisticsModel chapterStatisticsModel = new TopicStatisticsModel();
            DateTime date = DateTime.Now.AddSeconds(-ChapterController.CacheTimeInSeconds + 1);
            int periodId = Random.Shared.NextPositive();
            string expectedCacheKey = $"GetChapterStatistics-{existingChapter.CourseId}-{existingChapter.Code}-{periodId}";
            

            _chapterServiceMock.Setup(service => service.LoadChapterAsync(It.IsAny<int>(), It.IsAny<Code>(), It.IsAny<int?>()))
                .ReturnsAsync(existingChapter);

            _chapterServiceMock.Setup(service => service.GetChapterStatisticsAsync(It.IsAny<Chapter>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(chapterStatistics);

            _topicConverterMock.Setup(converter => converter.ToTopicStatisticsModel(It.IsAny<Chapter>(), It.IsAny<IReadOnlyList<AssignmentStatisticsDto>>(), It.IsAny<string>()))
                .Returns(chapterStatisticsModel);

            object cachedChapterStatisticsModel;
            _memoryCacheMock.Setup(cache => cache.TryGetValue(It.IsAny<object>(), out cachedChapterStatisticsModel))
                .Returns(false);

            Mock<ICacheEntry> cacheEntryMock = new Mock<ICacheEntry>();
            _memoryCacheMock.Setup(cache => cache.CreateEntry(It.IsAny<object>())).Returns(cacheEntryMock.Object);

            //Act
            OkObjectResult actionResult =
                _controller.GetChapterStatistics(existingChapter.CourseId, existingChapter.Code, date, periodId)
                    .Result as OkObjectResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            _memoryCacheMock.Verify(cache => cache.TryGetValue(expectedCacheKey, out cachedChapterStatisticsModel), Times.Once);
            _memoryCacheMock.Verify(cache => cache.CreateEntry(expectedCacheKey), Times.Once);

            Assert.That(actionResult.Value, Is.EqualTo(chapterStatisticsModel));
        }
    }
}
