using System;
using Guts.Business.Repositories;
using Guts.Business.Services;
using Guts.Common.Extensions;
using Guts.Domain.TestRunAggregate;
using Moq;
using NUnit.Framework;

namespace Guts.Business.Tests.Services
{
    [TestFixture]
    public class TestRunServiceTests
    {
        private TestRunService _service;
        private Random _random;
        private Mock<ITestRunRepository> _testRunRepositoryMock;

        [SetUp]
        public void Setup()
        {
            _random = new Random();
            _testRunRepositoryMock = new Mock<ITestRunRepository>();

            _service = new TestRunService(_testRunRepositoryMock.Object);
        }

        [Test]
        public void RegisterRunAsyncShouldSaveTheRunInTheRepository()
        {
            //Arrange
            var toBeSavedTestRun = new TestRun();

            var savedTestRun = new TestRun
            {
                Id = _random.NextPositive()
            };
            _testRunRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<TestRun>())).ReturnsAsync(savedTestRun);

            //Act
            var result = _service.RegisterRunAsync(toBeSavedTestRun).Result;

            //Assert
            Assert.That(result, Is.Not.Null);
            _testRunRepositoryMock.Verify(repo => repo.AddAsync(toBeSavedTestRun), Times.Once);
            Assert.That(result, Is.EqualTo(savedTestRun));
        }

        [Test]
        public void GetTestRunAsyncShouldRetrieveItFromTheRepositoryAndReturnIt()
        {
            //Arrange
            var storedTestRun = new TestRun
            {
                Id = _random.NextPositive()
            };

            _testRunRepositoryMock.Setup(repo => repo.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(storedTestRun);

            //Act
            var result = _service.GetTestRunAsync(storedTestRun.Id).Result;

            //Assert
            Assert.That(result, Is.Not.Null);
            _testRunRepositoryMock.Verify(repo => repo.GetByIdAsync(storedTestRun.Id), Times.Once);
            Assert.That(result, Is.EqualTo(storedTestRun));
        }
    }
}