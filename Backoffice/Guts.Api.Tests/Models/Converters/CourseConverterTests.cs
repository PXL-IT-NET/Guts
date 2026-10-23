using System;
using System.Collections.Generic;
using Guts.Api.Models.Converters;
using Guts.Business.Dtos;
using Guts.Business.Tests.Builders;
using Guts.Domain.TopicAggregate.ChapterAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Moq;
using NUnit.Framework;

namespace Guts.Api.Tests.Models.Converters
{
    [TestFixture]
    internal class CourseConverterTests
    {
        private CourseConverter _converter;
        private Mock<IChapterConverter> _chapterConverter;
        private Mock<IProjectConverter> _projectConverter;

        [SetUp]
        public void Setup()
        {
            _chapterConverter = new Mock<IChapterConverter>();
            _projectConverter = new Mock<IProjectConverter>();
            _converter = new CourseConverter(_chapterConverter.Object, _projectConverter.Object);
        }

        [Test]
        public void ToCourseContentsModel_ShouldCorrectlyConvertValidData()
        {
            //Arrange
            var course = new CourseBuilder().WithId().Build();
            var chapters = new List<Chapter>
            {
                new ChapterBuilder().Build(),
                new ChapterBuilder().Build()
            };

            var projects = new List<Project>
            {
                new ProjectBuilder().Build(),
                new ProjectBuilder().Build()
            };

            //Act
            var model = _converter.ToCourseContentsModel(course, chapters, projects);

            //Assert
            Assert.That(model, Is.Not.Null);
            Assert.That(model.Id, Is.EqualTo(course.Id));
            Assert.That(model.Code, Is.EqualTo(course.Code));
            Assert.That(model.Name, Is.EqualTo(course.Name));

            Assert.That(model.Chapters, Is.Not.Null);
            Assert.That(model.Chapters, Has.Count.EqualTo(chapters.Count));
            foreach (var chapter in chapters)
            {
                _chapterConverter.Verify(converter => converter.ToTopicModel(chapter), Times.Once);
            }

            Assert.That(model.Projects, Is.Not.Null);
            Assert.That(model.Projects, Has.Count.EqualTo(projects.Count));
            foreach (var project in projects)
            {
                _projectConverter.Verify(converter => converter.ToTopicModel(project), Times.Once);
            }
        }

        [Test]
        public void ToCourseContentsModel_ShouldThrowArgumentExceptionWhenCourseIsMissing()
        {
            //Arrange
            var chapters = new List<Chapter>();
            var projects = new List<Project>();

            //Act + Assert
            Assert.That(() => _converter.ToCourseContentsModel(null, chapters, projects), Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void ToCourseContentsModel_ShouldThrowArgumentExceptionWhenChaptersAreMissing()
        {
            //Arrange
            var course = new CourseBuilder().WithId().Build();
            var projects = new List<Project>();

            //Act + Assert
            Assert.That(() => _converter.ToCourseContentsModel(course, null, projects), Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void ToCourseContentsModel_ShouldThrowArgumentExceptionWhenProjectsAreMissing()
        {
            //Arrange
            var course = new CourseBuilder().WithId().Build();
            var chapters = new List<Chapter>();

            //Act + Assert
            Assert.That(() => _converter.ToCourseContentsModel(course, chapters, null), Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void ToCourseStatisticsModel_ShouldThrowArgumentNullExceptionWhenDtoIsNull()
        {
            Assert.That(() => _converter.ToCourseStatisticsModel(null), Throws.InstanceOf<ArgumentNullException>());
        }

        [Test]
        public void ToCourseStatisticsModel_ShouldCorrectlyConvertDto()
        {
            // Arrange
            var dto = new CourseStatisticsDto
            {
                CourseId = 1,
                CourseCode = "CS101",
                CourseName = "Computer Science",
                TotalAssignments = 5,
                TotalTests = 20,
                Students = new List<CourseStudentStatisticsDto>
                {
                    new CourseStudentStatisticsDto
                    {
                        StudentId = 42,
                        FirstName = "John",
                        LastName = "Doe",
                        AssignmentsWorkedOn = 4,
                        TotalPassedTests = 18,
                        TotalTests = 20,
                        CompletionPercentage = 90.0,
                        Chapters = new List<CourseStudentChapterStatisticsDto>
                        {
                            new CourseStudentChapterStatisticsDto
                            {
                                ChapterId = 10,
                                Code = "ch01",
                                Description = "Intro",
                                Assignments = new List<CourseStudentAssignmentStatisticsDto>
                                {
                                    new CourseStudentAssignmentStatisticsDto
                                    {
                                        AssignmentId = 101,
                                        Code = "ex01",
                                        Description = "Hello World",
                                        NumberOfPassedTests = 5,
                                        NumberOfTests = 5
                                    }
                                }
                            }
                        }
                    }
                }
            };

            // Act
            var model = _converter.ToCourseStatisticsModel(dto);

            // Assert
            Assert.That(model.CourseId, Is.EqualTo(dto.CourseId));
            Assert.That(model.CourseCode, Is.EqualTo(dto.CourseCode));
            Assert.That(model.CourseName, Is.EqualTo(dto.CourseName));
            Assert.That(model.TotalAssignments, Is.EqualTo(dto.TotalAssignments));
            Assert.That(model.TotalTests, Is.EqualTo(dto.TotalTests));
            Assert.That(model.Students, Has.Count.EqualTo(1));

            var student = model.Students[0];
            Assert.That(student.StudentId, Is.EqualTo(42));
            Assert.That(student.FirstName, Is.EqualTo("John"));
            Assert.That(student.LastName, Is.EqualTo("Doe"));
            Assert.That(student.FullName, Is.EqualTo("John Doe"));
            Assert.That(student.AssignmentsWorkedOn, Is.EqualTo(4));
            Assert.That(student.TotalPassedTests, Is.EqualTo(18));
            Assert.That(student.TotalTests, Is.EqualTo(20));
            Assert.That(student.CompletionPercentage, Is.EqualTo(90.0));
            Assert.That(student.Chapters, Has.Count.EqualTo(1));

            var chapter = student.Chapters[0];
            Assert.That(chapter.ChapterId, Is.EqualTo(10));
            Assert.That(chapter.Code, Is.EqualTo("ch01"));
            Assert.That(chapter.Assignments, Has.Count.EqualTo(1));

            var assignment = chapter.Assignments[0];
            Assert.That(assignment.AssignmentId, Is.EqualTo(101));
            Assert.That(assignment.Code, Is.EqualTo("ex01"));
            Assert.That(assignment.NumberOfPassedTests, Is.EqualTo(5));
            Assert.That(assignment.NumberOfTests, Is.EqualTo(5));
        }
    }
}
