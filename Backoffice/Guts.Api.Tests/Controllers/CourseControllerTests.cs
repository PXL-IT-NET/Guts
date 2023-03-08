using System.Collections.Generic;
using Guts.Api.Controllers;
using Guts.Api.Models;
using Guts.Api.Models.Converters;
using Guts.Business.Repositories;
using Guts.Business.Services;
using Guts.Business.Tests.Builders;
using Guts.Domain.CourseAggregate;
using Guts.Domain.TopicAggregate.ChapterAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using ControllerBase = Guts.Api.Controllers.ControllerBase;

namespace Guts.Api.Tests.Controllers
{
    [TestFixture]
    public class CourseControllerTests
    {
        private CourseController _controller;
        private Mock<ICourseService> _courseServiceMock;
        private Mock<IChapterService> _chapterServiceMock;
        private Mock<ICourseConverter> _courseConverterMock;
        private Mock<IProjectService> _projectServiceMock;


        [SetUp]
        public void Setup()
        {
            _courseServiceMock = new Mock<ICourseService>();
            _chapterServiceMock = new Mock<IChapterService>();
            _projectServiceMock = new Mock<IProjectService>();
            _courseConverterMock = new Mock<ICourseConverter>();
            _controller = new CourseController(_courseServiceMock.Object, 
                _chapterServiceMock.Object,
                _projectServiceMock.Object,
                _courseConverterMock.Object);
        }

        [Test]
        public void ShouldInheritFromControllerBase()
        {
            Assert.That(_controller, Is.InstanceOf<ControllerBase>());
        }

        [Test]
        public void GetCoursesShouldReturnTheCoursesReturnedByCourseService()
        {
            //Arrange
            var allCourses = new List<Course>();
            _courseServiceMock.Setup(service => service.GetAllCoursesAsync()).ReturnsAsync(allCourses);

            //Act
            var actionResult = _controller.GetCourses().Result as OkObjectResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            _courseServiceMock.Verify(service => service.GetAllCoursesAsync(), Times.Once);
            Assert.That(actionResult.Value, Is.EqualTo(allCourses));
        }

        [Test]
        public void GetCourseContentsShouldReturnBadRequestForInvalidCourseId()
        {
            //Arrange
            var courseId = -1;

            //Act
            var actionResult = _controller.GetCourseContents(courseId).Result as BadRequestResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
        }

        [Test]
        public void GetCourseContentsShouldGetTheChaptersFromTheRepositoryAndConvertThemToModels()
        {
            //Arrange
            var existingCourse = new CourseBuilder().WithId().Build();
            _courseServiceMock.Setup(service => service.GetCourseByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(existingCourse);

            var existingChapters = new List<Chapter>
            {
                new ChapterBuilder().Build(),
                new ChapterBuilder().Build(),
            };
            _chapterServiceMock.Setup(service => service.GetChaptersOfCourseAsync(It.IsAny<int>())).ReturnsAsync(existingChapters);

            var convertedCourse = new CourseContentsModel();
            _courseConverterMock
                .Setup(converter => converter.ToCourseContentsModel(It.IsAny<Course>(), It.IsAny<IReadOnlyList<Chapter>>(), It.IsAny<IReadOnlyList<Project>>()))
                .Returns(convertedCourse);

            //Act
            var actionResult = _controller.GetCourseContents(existingCourse.Id).Result as OkObjectResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            _chapterServiceMock.Verify(service => service.GetChaptersOfCourseAsync(existingCourse.Id), Times.Once);
            _courseConverterMock.Verify(converter => converter.ToCourseContentsModel(existingCourse, existingChapters, It.IsAny<IReadOnlyList<Project>>()), Times.Once);
            Assert.That(actionResult.Value, Is.EqualTo(convertedCourse));
        }

        [Test]
        public void GetCourseContentsShouldGetTheProjectsFromTheRepositoryAndConvertThemToModels()
        {
            //Arrange
            var existingCourse = new CourseBuilder().WithId().Build();
            _courseServiceMock.Setup(service => service.GetCourseByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(existingCourse);

            var existingProjects = new List<Project>
            {
                new ProjectBuilder().Build(),
                new ProjectBuilder().Build(),
            };
            _projectServiceMock.Setup(service => service.GetProjectsOfCourseAsync(It.IsAny<int>())).ReturnsAsync(existingProjects);

            var convertedCourse = new CourseContentsModel();
            _courseConverterMock
                .Setup(converter => converter.ToCourseContentsModel(It.IsAny<Course>(), It.IsAny<IReadOnlyList<Chapter>>(), It.IsAny<IReadOnlyList<Project>>()))
                .Returns(convertedCourse);

            //Act
            var actionResult = _controller.GetCourseContents(existingCourse.Id).Result as OkObjectResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            _projectServiceMock.Verify(service => service.GetProjectsOfCourseAsync(existingCourse.Id), Times.Once);
            _courseConverterMock.Verify(converter => converter.ToCourseContentsModel(existingCourse, It.IsAny<IReadOnlyList<Chapter>>(), existingProjects), Times.Once);
            Assert.That(actionResult.Value, Is.EqualTo(convertedCourse));
        }

    }
}
