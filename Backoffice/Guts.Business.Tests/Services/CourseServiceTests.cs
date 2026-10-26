using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Business.Repositories;
using Guts.Business.Services;
using Guts.Business.Tests.Builders;
using Guts.Domain.Tests.Builders;
using Guts.Domain.CourseAggregate;
using Guts.Domain.TestRunAggregate;
using Guts.Domain.TopicAggregate.ChapterAggregate;
using Guts.Domain.UserAggregate;
using Moq;
using NUnit.Framework;

namespace Guts.Business.Tests.Services
{
    [TestFixture]
    public class CourseServiceTests
    {
        private CourseService _service;

        private Mock<ICourseRepository> _courseRepositoryMock;
        private Mock<IPeriodRepository> _periodRepositoryMock;
        private Mock<IChapterRepository> _chapterRepositoryMock;
        private Mock<ITestResultRepository> _testResultRepositoryMock;

        [SetUp]
        public void Setup()
        {
            _courseRepositoryMock = new Mock<ICourseRepository>();
            _periodRepositoryMock = new Mock<IPeriodRepository>();
            _chapterRepositoryMock = new Mock<IChapterRepository>();
            _testResultRepositoryMock = new Mock<ITestResultRepository>();

            _service = new CourseService(
                _courseRepositoryMock.Object,
                _periodRepositoryMock.Object,
                _chapterRepositoryMock.Object,
                _testResultRepositoryMock.Object);
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

        [Test]
        public async Task GetCourseStatisticsAsync_ShouldReturnEmptyStudents_WhenNoAssignmentsExist()
        {
            // Arrange
            var course = new CourseBuilder().WithId().Build();
            var period = new PeriodBuilder().WithId().Build();

            _courseRepositoryMock.Setup(repo => repo.GetByIdAsync(course.Id)).ReturnsAsync(course);
            _periodRepositoryMock.Setup(repo => repo.GetPeriodAsync(null)).ReturnsAsync(period);
            _chapterRepositoryMock.Setup(repo => repo.GetByCourseIdWithAssignmentsAndTestsAsync(course.Id, period.Id))
                .ReturnsAsync(new List<Chapter>());

            // Act
            var result = await _service.GetCourseStatisticsAsync(course.Id);

            // Assert
            Assert.That(result.CourseId, Is.EqualTo(course.Id));
            Assert.That(result.CourseCode, Is.EqualTo(course.Code));
            Assert.That(result.TotalAssignments, Is.EqualTo(0));
            Assert.That(result.TotalTests, Is.EqualTo(0));
            Assert.That(result.Students, Is.Empty);
        }

        [Test]
        public async Task GetCourseStatisticsAsync_ShouldCalculateStatisticsAndSortStudentsByLastNameThenFirstName()
        {
            // Arrange
            var course = new CourseBuilder().WithId().Build();
            var period = new PeriodBuilder().WithId().Build();

            var chapter1 = new ChapterBuilder().WithId().WithCode("ch01").WithAssignments(2, 2).Build();
            var chapter2 = new ChapterBuilder().WithId().WithCode("ch02").WithAssignments(1, 2).Build();
            var chapters = new List<Chapter> { chapter1, chapter2 };

            _courseRepositoryMock.Setup(repo => repo.GetByIdAsync(course.Id)).ReturnsAsync(course);
            _periodRepositoryMock.Setup(repo => repo.GetPeriodAsync(period.Id)).ReturnsAsync(period);
            _chapterRepositoryMock.Setup(repo => repo.GetByCourseIdWithAssignmentsAndTestsAsync(course.Id, period.Id))
                .ReturnsAsync(chapters);

            var studentB = new User { Id = 1, FirstName = "Bob", LastName = "Zeta" };
            var studentA = new User { Id = 2, FirstName = "Alice", LastName = "Alpha" };

            var assignment1 = chapter1.Assignments.ElementAt(0);
            var assignment2 = chapter1.Assignments.ElementAt(1);
            var assignment3 = chapter2.Assignments.ElementAt(0);

            var test1_1 = assignment1.Tests.ElementAt(0);
            var test1_2 = assignment1.Tests.ElementAt(1);
            var test2_1 = assignment2.Tests.ElementAt(0);
            var test2_2 = assignment2.Tests.ElementAt(1);
            var test3_1 = assignment3.Tests.ElementAt(0);
            var test3_2 = assignment3.Tests.ElementAt(1);

            var testResults = new List<TestResult>
            {
                // Student A worked on assignment 1 (2 passed) and assignment 3 (1 passed, 1 failed) -> 2 assignments worked on, 3 passed out of 6 total
                new TestResult { UserId = studentA.Id, User = studentA, TestId = test1_1.Id, Test = test1_1, Passed = true },
                new TestResult { UserId = studentA.Id, User = studentA, TestId = test1_2.Id, Test = test1_2, Passed = true },
                new TestResult { UserId = studentA.Id, User = studentA, TestId = test3_1.Id, Test = test3_1, Passed = true },
                new TestResult { UserId = studentA.Id, User = studentA, TestId = test3_2.Id, Test = test3_2, Passed = false },

                // Student B worked on assignment 1 (1 passed) -> 1 assignment worked on, 1 passed out of 6 total
                new TestResult { UserId = studentB.Id, User = studentB, TestId = test1_1.Id, Test = test1_1, Passed = true }
            };

            _testResultRepositoryMock.Setup(repo => repo.GetLastTestResultsOfAssignmentsAsync(It.IsAny<int[]>(), null))
                .ReturnsAsync(testResults);

            // Act
            var result = await _service.GetCourseStatisticsAsync(course.Id, period.Id);

            // Assert
            Assert.That(result.CourseId, Is.EqualTo(course.Id));
            Assert.That(result.TotalAssignments, Is.EqualTo(3));
            Assert.That(result.TotalTests, Is.EqualTo(6));
            Assert.That(result.Students, Has.Count.EqualTo(2));

            // Sorted by LastName, FirstName -> Alpha Alice first, then Zeta Bob
            var firstStudent = result.Students[0];
            Assert.That(firstStudent.StudentId, Is.EqualTo(studentA.Id));
            Assert.That(firstStudent.LastName, Is.EqualTo("Alpha"));
            Assert.That(firstStudent.FirstName, Is.EqualTo("Alice"));
            Assert.That(firstStudent.AssignmentsWorkedOn, Is.EqualTo(2));
            Assert.That(firstStudent.TotalPassedTests, Is.EqualTo(3));
            Assert.That(firstStudent.TotalTests, Is.EqualTo(6));
            Assert.That(firstStudent.CompletionPercentage, Is.EqualTo(50.0));
            Assert.That(firstStudent.Chapters, Has.Count.EqualTo(2));

            var secondStudent = result.Students[1];
            Assert.That(secondStudent.StudentId, Is.EqualTo(studentB.Id));
            Assert.That(secondStudent.LastName, Is.EqualTo("Zeta"));
            Assert.That(secondStudent.FirstName, Is.EqualTo("Bob"));
            Assert.That(secondStudent.AssignmentsWorkedOn, Is.EqualTo(1));
            Assert.That(secondStudent.TotalPassedTests, Is.EqualTo(1));
            Assert.That(secondStudent.TotalTests, Is.EqualTo(6));
            Assert.That(secondStudent.CompletionPercentage, Is.EqualTo(16.67));
        }
    }
}
