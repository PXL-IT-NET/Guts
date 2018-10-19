using System;
using System.Linq;
using Guts.Business.Services;
using Guts.Business.Tests.Builders;
using Guts.Common.Extensions;
using Guts.Data;
using Guts.Data.Repositories;
using Guts.Domain;
using Moq;
using NUnit.Framework;

namespace Guts.Business.Tests.Services
{
    [TestFixture]
    public class ProjectServiceTests
    {
        private ProjectService _service;
        private Random _random;
        private Mock<IProjectRepository> _projectRepositoryMock;
        private Mock<ICourseRepository> _courseRepositoryMock;
        private Mock<IPeriodRepository> _periodRepositoryMock;

        [SetUp]
        public void Setup()
        {
            _random = new Random();
            _projectRepositoryMock = new Mock<IProjectRepository>();
            _courseRepositoryMock = new Mock<ICourseRepository>();
            _periodRepositoryMock = new Mock<IPeriodRepository>();

            _service = new ProjectService(_projectRepositoryMock.Object, _courseRepositoryMock.Object, _periodRepositoryMock.Object);
        }

        [Test]
        public void GetProjectAsync_ShouldReturnProjectForCurrentPeriod()
        { 
            //Arrange
            var existingPeriod = new Period { Id = _random.NextPositive() };
            var courseCode = Guid.NewGuid().ToString();
            var existingProject = new ProjectBuilder().WithId()
                .WithCourse(courseCode)
                .WithPeriod(existingPeriod).Build();

            _periodRepositoryMock.Setup(repo => repo.GetCurrentPeriodAsync()).ReturnsAsync(existingPeriod);
            _projectRepositoryMock.Setup(repo => repo.GetSingleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(existingProject);

            //Act
            var result = _service.GetProjectAsync(courseCode, existingProject.Code).Result;

            //Assert
            _periodRepositoryMock.Verify(repo => repo.GetCurrentPeriodAsync(), Times.Once);
            _projectRepositoryMock.Verify(repo => repo.GetSingleAsync(courseCode, existingProject.Code, existingPeriod.Id), Times.Once());
            Assert.That(result, Is.EqualTo(existingProject));
        }

        [Test]
        public void GetOrCreateProjectAsync_ShouldReturnProjectIfItExists()
        {
            //Arrange
            var existingPeriod = new Period { Id = _random.NextPositive() };
            var courseCode = Guid.NewGuid().ToString();
            var existingProject = new ProjectBuilder().WithId()
                .WithCourse(courseCode)
                .WithPeriod(existingPeriod).Build();

            _periodRepositoryMock.Setup(repo => repo.GetCurrentPeriodAsync()).ReturnsAsync(existingPeriod);

            _projectRepositoryMock.Setup(repo => repo.GetSingleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(existingProject);

            //Act
            var result = _service.GetOrCreateProjectAsync(courseCode, existingProject.Code).Result;

            //Assert
            _projectRepositoryMock.Verify(repo => repo.GetSingleAsync(courseCode, existingProject.Code, existingPeriod.Id), Times.Once());
            _projectRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Project>()), Times.Never);
            Assert.That(result, Is.EqualTo(existingProject));
        }

        [Test]
        public void GetOrCreateProjectAsync_ShouldThrowDataNotFoundExeptionWhenNoCurrentPeriodIsFound()
        {
            //Arrange
            var courseCode = Guid.NewGuid().ToString();
            var existingProject = new ProjectBuilder().WithId().WithCourse(courseCode).Build();

            _periodRepositoryMock.Setup(repo => repo.GetCurrentPeriodAsync()).Throws<DataNotFoundException>();

            //Act + Assert
            Assert.That(() => _service.GetOrCreateProjectAsync(courseCode, existingProject.Code).Result,
                Throws.InstanceOf<AggregateException>().With.Matches((AggregateException ex) =>
                    ex.InnerExceptions.OfType<DataNotFoundException>().Any()));
        }

        [Test]
        public void GetOrCreateProjectAsync_ShouldCreateProjectIfItDoesNotExist()
        {
            //Arrange
            var existingPeriod = new Period { Id = _random.NextPositive() };
            var existingCourse = new CourseBuilder().WithId().Build();

            _periodRepositoryMock.Setup(repo => repo.GetCurrentPeriodAsync()).ReturnsAsync(existingPeriod);
            _courseRepositoryMock.Setup(repo => repo.GetSingleAsync(It.IsAny<string>())).ReturnsAsync(existingCourse);

            _projectRepositoryMock
                .Setup(repo => repo.GetSingleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Throws<DataNotFoundException>();

            var addedProject = new ProjectBuilder().WithId().Build();
            _projectRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<Project>())).ReturnsAsync(addedProject);

            //Act
            var result = _service.GetOrCreateProjectAsync(existingCourse.Code, addedProject.Code).Result;

            //Assert
            _periodRepositoryMock.Verify(repo => repo.GetCurrentPeriodAsync(), Times.Once);
            _projectRepositoryMock.Verify(repo => repo.GetSingleAsync(existingCourse.Code, addedProject.Code, existingPeriod.Id), Times.Once);
            _projectRepositoryMock.Verify(
                repo => repo.AddAsync(It.Is<Project>(p =>
                    p.PeriodId == existingPeriod.Id && p.CourseId == existingCourse.Id &&
                    p.Code == addedProject.Code)), Times.Once);

            _courseRepositoryMock.Verify(repo => repo.GetSingleAsync(existingCourse.Code), Times.Once);

            Assert.That(result, Is.EqualTo(addedProject));
        }
    }
}