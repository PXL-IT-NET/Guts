using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Business.Repositories;
using Guts.Business.Services;
using Guts.Common.Extensions;
using Guts.Domain.TestRunAggregate;
using Guts.Domain.Tests.Builders;
using Guts.Domain.ValueObjects;
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
        private Mock<ISolutionFileRepository> _solutionFileRepositoryMock;

        [SetUp]
        public void Setup()
        {
            _random = new Random();
            _testRunRepositoryMock = new Mock<ITestRunRepository>();
            _solutionFileRepositoryMock = new Mock<ISolutionFileRepository>();

            _service = new TestRunService(_testRunRepositoryMock.Object, _solutionFileRepositoryMock.Object);
        }

        [Test]
        public void RegisterRunAsync_ShouldSaveTheRunInTheRepository()
        {
            //Arrange
            var toBeSavedTestRun = new TestRun();

            var savedTestRun = new TestRun
            {
                Id = _random.NextPositive()
            };
            _testRunRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<TestRun>())).ReturnsAsync(savedTestRun);

            //Act
            var result = _service.RegisterRunAsync(toBeSavedTestRun, new List<SolutionFile>()).Result;

            //Assert
            Assert.That(result, Is.Not.Null);
            _testRunRepositoryMock.Verify(repo => repo.AddAsync(toBeSavedTestRun), Times.Once);
            Assert.That(result, Is.EqualTo(savedTestRun));
        }

        [Test]
        public void RegisterRunAsync_ShouldSaveNewerSolutionFile()
        {
            //Arrange
            int assignmentId = _random.NextPositive();
            int userId = _random.NextPositive();
            var solutionFiles = new List<SolutionFile>();
            int numberOfFiles = _random.Next(1, 5);
            for (int i = 0; i < numberOfFiles; i++)
            {
                solutionFiles.Add(new SolutionFileBuilder().WithUserId(userId).WithAssignmentId(assignmentId).Build());
            }

            DateTime minimumDate = solutionFiles.Min(sf => sf.ModifyDateTime);
            _solutionFileRepositoryMock
                .Setup(repo => repo.GetLatestForUserAsync(assignmentId, userId, It.IsAny<FilePath>()))
                .ReturnsAsync((int previousAssignmentId, int previousUserId, FilePath path) => new SolutionFileBuilder()
                    .WithUserId(previousUserId)
                    .WithAssignmentId(previousAssignmentId)
                    .WithPath(path)
                    .WithModifiedDateBefore(minimumDate)
                    .Build());

            var toBeSavedTestRun = new TestRun();
            _testRunRepositoryMock.Setup(repo => repo.AddAsync(toBeSavedTestRun));

            //Act
            _service.RegisterRunAsync(toBeSavedTestRun, solutionFiles).Wait();

            //Assert
            _testRunRepositoryMock.Verify();
            _solutionFileRepositoryMock.Verify(
                repo => repo.GetLatestForUserAsync(assignmentId, userId, It.IsAny<FilePath>()),
                Times.Exactly(solutionFiles.Count));
            _solutionFileRepositoryMock.Verify(
                repo => repo.AddAsync(It.IsIn<SolutionFile>(solutionFiles)),
                Times.Exactly(solutionFiles.Count));
        }

        [Test]
        public void RegisterRunAsync_ShouldNotAddUnchangedSolutionFile()
        {
            //Arrange
            int assignmentId = _random.NextPositive();
            int userId = _random.NextPositive();
            SolutionFile solutionFile =
                new SolutionFileBuilder().WithUserId(userId).WithAssignmentId(assignmentId).Build();
            var solutionFiles = new List<SolutionFile>{ solutionFile};

            _solutionFileRepositoryMock
                .Setup(repo => repo.GetLatestForUserAsync(assignmentId, userId, It.IsAny<FilePath>()))
                .ReturnsAsync((int previousAssignmentId, int previousUserId, FilePath path) => new SolutionFileBuilder()
                    .WithUserId(previousUserId)
                    .WithAssignmentId(previousAssignmentId)
                    .WithPath(path)
                    .WithContent(solutionFile.Content)
                    .WithModifiedDateBefore(solutionFile.ModifyDateTime)
                    .Build());

            var toBeSavedTestRun = new TestRun();
            _testRunRepositoryMock.Setup(repo => repo.AddAsync(toBeSavedTestRun));

            //Act
            _service.RegisterRunAsync(toBeSavedTestRun, solutionFiles).Wait();

            //Assert
            _testRunRepositoryMock.Verify();
            _solutionFileRepositoryMock.Verify(
                repo => repo.GetLatestForUserAsync(assignmentId, userId, It.IsAny<FilePath>()),
                Times.Exactly(solutionFiles.Count));
            _solutionFileRepositoryMock.Verify(
                repo => repo.AddAsync(It.IsIn<SolutionFile>(solutionFiles)),
                Times.Never);
        }

        [Test]
        public void GetTestRunAsync_ShouldRetrieveItFromTheRepositoryAndReturnIt()
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