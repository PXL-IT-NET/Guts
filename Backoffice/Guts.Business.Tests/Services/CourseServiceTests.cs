using System.Collections.Generic;
using Guts.Business.Repositories;
using Guts.Business.Services;
using Guts.Business.Tests.Builders;
using Guts.Domain.CourseAggregate;
using Moq;
using NUnit.Framework;

namespace Guts.Business.Tests.Services
{
    [TestFixture]
    public class CourseServiceTests
    {
        private CourseService _service;

        private Mock<ICourseRepository> _courseRepositoryMock;

        [SetUp]
        public void Setup()
        {
            _courseRepositoryMock = new Mock<ICourseRepository>();
            _service = new CourseService(_courseRepositoryMock.Object);
        }

        [Test]
        public void GetAllCoursesAsyncShouldReturnAllCoursesFromRepository()
        {
            //Arrange
           var allCourses = new List<Course>();
            _courseRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(allCourses);

            //Act
            var result = _service.GetAllCoursesAsync().Result;

            //Assert
            _courseRepositoryMock.Verify(repo => repo.GetAllAsync(), Times.Once);
            Assert.That(result, Is.EqualTo(allCourses));
        }

        [Test]
        public void GetCourseByIdAsync_ShouldUseRepository()
        {
            //Arrange
            var existingCourse = new CourseBuilder().WithId().Build();

            _courseRepositoryMock.Setup(repo => repo.GetByIdAsync(existingCourse.Id)).ReturnsAsync(existingCourse);

            //Act
            var result = _service.GetCourseByIdAsync(existingCourse.Id).Result;

            //Assert
            _courseRepositoryMock.Verify();
            Assert.That(result, Is.SameAs(existingCourse));
        }
    }
}