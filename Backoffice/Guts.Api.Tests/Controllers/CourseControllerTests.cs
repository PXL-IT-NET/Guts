using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Guts.Api.Controllers;
using Guts.Api.Models;
using Guts.Api.Models.Converters;
using Guts.Application;
using Guts.Application.Dtos;
using Guts.Application.Services;
using Guts.Application.Tests.Builders;
using Guts.Domain.Tests.Builders;
using Guts.Common.Extensions;
using Guts.Domain.CourseAggregate;
using Guts.Domain.TopicAggregate.ChapterAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using ControllerBase = Guts.Api.Controllers.ControllerBase;

namespace Guts.Api.Tests.Controllers;

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
        _courseServiceMock = new();
        _chapterServiceMock = new();
        _projectServiceMock = new();
        _courseConverterMock = new();
        _controller = new(_courseServiceMock.Object, 
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
        List<Course> allCourses = [];
        _courseServiceMock.Setup(service => service.GetAllCoursesAsync()).ReturnsAsync(allCourses);

        //Act
        OkObjectResult actionResult = _controller.GetCourses().Result as OkObjectResult;

        //Assert
        Assert.That(actionResult, Is.Not.Null);
        _courseServiceMock.Verify(service => service.GetAllCoursesAsync(), Times.Once);
        Assert.That(actionResult.Value, Is.EqualTo(allCourses));
    }

    [Test]
    public void GetCourseContentsShouldReturnBadRequestForInvalidCourseId()
    {
        //Arrange
        int courseId = -1;

        //Act
        BadRequestResult actionResult = _controller.GetCourseContents(courseId).Result as BadRequestResult;

        //Assert
        Assert.That(actionResult, Is.Not.Null);
    }

    [Test]
    public void GetCourseContentsShouldGetTheChaptersFromTheRepositoryAndConvertThemToModels()
    {
        //Arrange
        int periodId = Random.Shared.NextPositive();
        Course existingCourse = new CourseBuilder().WithId().Build();
        _courseServiceMock.Setup(service => service.GetCourseByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(existingCourse);

        List<Chapter> existingChapters =
        [
            new ChapterBuilder().Build(),
            new ChapterBuilder().Build()
        ];
        _chapterServiceMock.Setup(service => service.GetChaptersOfCourseAsync(It.IsAny<int>(), It.IsAny<int?>())).ReturnsAsync(existingChapters);

        CourseContentsModel convertedCourse = new();
        _courseConverterMock
            .Setup(converter => converter.ToCourseContentsModel(It.IsAny<Course>(), It.IsAny<IReadOnlyList<Chapter>>(), It.IsAny<IReadOnlyList<Project>>()))
            .Returns(convertedCourse);

        //Act
        OkObjectResult actionResult = _controller.GetCourseContents(existingCourse.Id, periodId).Result as OkObjectResult;

        //Assert
        Assert.That(actionResult, Is.Not.Null);
        _chapterServiceMock.Verify(service => service.GetChaptersOfCourseAsync(existingCourse.Id, periodId), Times.Once);
        _courseConverterMock.Verify(converter => converter.ToCourseContentsModel(existingCourse, existingChapters, It.IsAny<IReadOnlyList<Project>>()), Times.Once);
        Assert.That(actionResult.Value, Is.EqualTo(convertedCourse));
    }

    [Test]
    public void GetCourseContentsShouldGetTheProjectsFromTheRepositoryAndConvertThemToModels()
    {
        //Arrange
        int periodId = Random.Shared.NextPositive();
        Course existingCourse = new CourseBuilder().WithId().Build();
        _courseServiceMock.Setup(service => service.GetCourseByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(existingCourse);

        List<Project> existingProjects =
        [
            new ProjectBuilder().Build(),
            new ProjectBuilder().Build()
        ];
        _projectServiceMock.Setup(service => service.GetProjectsOfCourseAsync(It.IsAny<int>(), It.IsAny<int?>())).ReturnsAsync(existingProjects);

        CourseContentsModel convertedCourse = new();
        _courseConverterMock
            .Setup(converter => converter.ToCourseContentsModel(It.IsAny<Course>(), It.IsAny<IReadOnlyList<Chapter>>(), It.IsAny<IReadOnlyList<Project>>()))
            .Returns(convertedCourse);

        //Act
        OkObjectResult actionResult = _controller.GetCourseContents(existingCourse.Id, periodId).Result as OkObjectResult;

        //Assert
        Assert.That(actionResult, Is.Not.Null);
        _projectServiceMock.Verify(service => service.GetProjectsOfCourseAsync(existingCourse.Id, periodId), Times.Once);
        _courseConverterMock.Verify(converter => converter.ToCourseContentsModel(existingCourse, It.IsAny<IReadOnlyList<Chapter>>(), existingProjects), Times.Once);
        Assert.That(actionResult.Value, Is.EqualTo(convertedCourse));
    }

    [Test]
    public async Task GetCourseStatistics_InvalidCourseId_ShouldReturnBadRequest()
    {
        var result = await _controller.GetCourseStatistics(0) as BadRequestResult;
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public async Task GetCourseStatistics_WhenCourseNotFound_ShouldReturnNotFound()
    {
        _courseServiceMock.Setup(s => s.GetCourseStatisticsAsync(It.IsAny<int>(), It.IsAny<int?>()))
            .ThrowsAsync(new DataNotFoundException());

        var result = await _controller.GetCourseStatistics(10, 5) as NotFoundResult;
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public async Task GetCourseStatistics_ValidRequest_ShouldReturnOkWithConvertedModel()
    {
        int courseId = 12;
        int periodId = 3;
        var dto = new CourseStatisticsDto { CourseId = courseId };
        var model = new CourseStatisticsModel { CourseId = courseId };

        _courseServiceMock.Setup(s => s.GetCourseStatisticsAsync(courseId, periodId))
            .ReturnsAsync(dto);
        _courseConverterMock.Setup(c => c.ToCourseStatisticsModel(dto))
            .Returns(model);

        var actionResult = await _controller.GetCourseStatistics(courseId, periodId) as OkObjectResult;
        Assert.That(actionResult, Is.Not.Null);
        Assert.That(actionResult.Value, Is.SameAs(model));
    }

    [Test]
    public void GetCourseStatistics_ShouldHaveLectorsOnlyPolicy()
    {
        var method = typeof(CourseController).GetMethod(nameof(CourseController.GetCourseStatistics));
        var authAttribute = method?.GetCustomAttribute<AuthorizeAttribute>();
        Assert.That(authAttribute, Is.Not.Null);
        Assert.That(authAttribute.Policy, Is.EqualTo(ApiConstants.LectorsOnlyPolicy));
    }

    [TestCase(nameof(CourseController.CreateCourse))]
    [TestCase(nameof(CourseController.UpdateCourse))]
    [TestCase(nameof(CourseController.DeleteCourse))]
    public void MutationEndpoints_ShouldHaveAdminOnlyPolicy(string methodName)
    {
        var method = typeof(CourseController).GetMethod(methodName);
        var attr = method?.GetCustomAttribute<AuthorizeAttribute>();
        Assert.That(attr, Is.Not.Null);
        Assert.That(attr.Policy, Is.EqualTo(ApiConstants.AdminOnlyPolicy));
    }

    [Test]
    public async Task CreateCourse_Valid_ReturnsCreated()
    {
        var created = new Course("CS101", "Intro") { Id = 1 };
        _courseServiceMock.Setup(s => s.CreateCourseAsync("CS101", "Intro")).ReturnsAsync(created);

        var result = await _controller.CreateCourse(new CourseSaveModel { Code = "CS101", Name = "Intro" }) as CreatedAtActionResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Is.SameAs(created));
    }

    [Test]
    public async Task UpdateCourse_Valid_ReturnsOk()
    {
        var updated = new Course("CS102", "Advanced") { Id = 1 };
        _courseServiceMock.Setup(s => s.UpdateCourseAsync(1, "CS102", "Advanced")).ReturnsAsync(updated);

        var result = await _controller.UpdateCourse(1, new CourseSaveModel { Code = "CS102", Name = "Advanced" }) as OkObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Is.SameAs(updated));
    }

    [Test]
    public async Task DeleteCourse_Valid_ReturnsNoContent()
    {
        var result = await _controller.DeleteCourse(1);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        _courseServiceMock.Verify(s => s.DeleteCourseAsync(1), Times.Once);
    }
}