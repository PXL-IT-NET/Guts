using System;
using System.Collections.Generic;
using Guts.Business.Repositories;
using Guts.Business.Services;
using Guts.Business.Tests.Builders;
using Guts.Common.Extensions;
using Guts.Domain.PeriodAggregate;
using Guts.Domain.TopicAggregate;
using Moq;
using NUnit.Framework;

namespace Guts.Business.Tests.Services
{
    [TestFixture]
    public class TopicServiceTests
    {
        private TopicService _service;

        private Mock<ITopicRepository> _topicRepositoryMock;
        private Mock<IPeriodRepository> _periodRepositoryMock;
        private Random _random;

        [SetUp]
        public void Setup()
        {
            _random = new Random();
            _periodRepositoryMock = new Mock<IPeriodRepository>();
            _topicRepositoryMock = new Mock<ITopicRepository>();
            _service = new TopicService(_topicRepositoryMock.Object, _periodRepositoryMock.Object);
        }

        [Test]
        public void GetTopicAsync_ShouldGetCurrentPeriodAndUseRepository()
        {
            //Arrange
            var courseCode = _random.NextString();
            var existingPeriod = new Period { Id = _random.NextPositive() };
            var existingTopic = new ChapterBuilder().WithId().Build();
            

            _periodRepositoryMock.Setup(repo => repo.GetCurrentPeriodAsync()).ReturnsAsync(existingPeriod);
            _topicRepositoryMock.Setup(repo => repo.GetSingleAsync(courseCode, existingTopic.Code, existingPeriod.Id)).ReturnsAsync(existingTopic);

            //Act
            var result = _service.GetTopicAsync(courseCode, existingTopic.Code).Result;

            //Assert
            _periodRepositoryMock.Verify();
            _topicRepositoryMock.Verify();
            Assert.That(result, Is.SameAs(existingTopic));
        }

        [Test]
        public void GetTopicsByCourseWithAssignmentsAndTestsAsync_ShouldGetCurrentPeriodAndUseRepository()
        {
            //Arrange
            var existingPeriod = new Period { Id = _random.NextPositive() };
            var existingTopics = new List<Topic>();
            var courseId = _random.NextPositive();

            _periodRepositoryMock.Setup(repo => repo.GetCurrentPeriodAsync()).ReturnsAsync(existingPeriod);
            _topicRepositoryMock
                .Setup(repo => repo.GetByCourseWithAssignmentsAndTestsAsync(courseId, existingPeriod.Id))
                .ReturnsAsync(existingTopics);

            //Act
            var results = _service.GetTopicsByCourseWithAssignmentsAndTestsAsync(courseId).Result;

            //Assert
            _periodRepositoryMock.Verify();
            _topicRepositoryMock.Verify();
            Assert.That(results, Is.SameAs(existingTopics));
        }
    }
}