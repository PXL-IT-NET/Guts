using System;
using System.Collections.Generic;
using Guts.Business.Repositories;
using Guts.Business.Services;
using Guts.Business.Tests.Builders;
using Guts.Common.Extensions;
using Guts.Domain.PeriodAggregate;
using Guts.Domain.Tests.Builders;
using Guts.Domain.TopicAggregate;
using Guts.Domain.TopicAggregate.ChapterAggregate;
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

        [SetUp]
        public void Setup()
        {
            _periodRepositoryMock = new Mock<IPeriodRepository>();
            _topicRepositoryMock = new Mock<ITopicRepository>();
            _service = new TopicService(_topicRepositoryMock.Object, _periodRepositoryMock.Object);
        }

        [Test]
        public void GetTopicAsync_ShouldGetCurrentPeriodAndUseRepository()
        {
            //Arrange
            string courseCode = Random.Shared.NextString();
            Period existingPeriod = new PeriodBuilder().WithId().Build();
            Chapter existingTopic = new ChapterBuilder().WithId().Build();
            

            _periodRepositoryMock.Setup(repo => repo.GetPeriodAsync(existingPeriod.Id)).ReturnsAsync(existingPeriod);
            _topicRepositoryMock.Setup(repo => repo.GetSingleAsync(courseCode, existingTopic.Code, existingPeriod.Id)).ReturnsAsync(existingTopic);

            //Act
            ITopic result = _service.GetTopicAsync(courseCode, existingTopic.Code, existingPeriod.Id).Result;

            //Assert
            _periodRepositoryMock.Verify();
            _topicRepositoryMock.Verify();
            Assert.That(result, Is.SameAs(existingTopic));
        }

        [Test]
        public void GetTopicsByCourseWithAssignmentsAndTestsAsync_ShouldGetCurrentPeriodAndUseRepository()
        {
            //Arrange
            Period existingPeriod = new PeriodBuilder().WithId().Build();
            List<Topic> existingTopics = new List<Topic>();
            var courseId = Random.Shared.NextPositive();

            _periodRepositoryMock.Setup(repo => repo.GetPeriodAsync(null)).ReturnsAsync(existingPeriod);
            _topicRepositoryMock
                .Setup(repo => repo.GetByCourseWithAssignmentsAndTestsAsync(courseId, existingPeriod.Id))
                .ReturnsAsync(existingTopics);

            //Act
            IReadOnlyList<ITopic> results = _service.GetTopicsByCourseWithAssignmentsAndTestsAsync(courseId, null).Result;

            //Assert
            _periodRepositoryMock.Verify();
            _topicRepositoryMock.Verify();
            Assert.That(results, Is.SameAs(existingTopics));
        }
    }
}