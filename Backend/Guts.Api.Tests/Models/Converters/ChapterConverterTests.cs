using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Api.Models.Converters;
using Guts.Business;
using Guts.Business.Tests.Builders;
using Guts.Domain;
using Moq;
using NUnit.Framework;

namespace Guts.Api.Tests.Models.Converters
{
    [TestFixture]
    internal class ChapterConverterTests
    {
        private ChapterConverter _converter;

        [SetUp]
        public void Setup()
        {
            var userConverterMock = new Mock<IUserConverter>();
            _converter = new ChapterConverter(userConverterMock.Object);
        }

        [Test]
        [TestCase(5, 5, 0, 10)]
        [TestCase(5, 0, 5, 1)]
        [TestCase(5, 1, 1, 10)]
        public void ToChapterSummaryModel_ShouldCorrectlyConvertValidChapter(int numberOfTests,
            int numberOfPassingTests,
            int numberOfFailingTests,
            int numberOfUsers)
        {
            //Arrange
            var chapter = new ChapterBuilder().WithId().WithAssignments(5, numberOfTests).Build();
            var userAssignmentResults = GenerateAssignmentResults(chapter, numberOfPassingTests, numberOfFailingTests);

            //Act
            var model = _converter.ToChapterSummaryModel(chapter, userAssignmentResults);

            //Assert
            Assert.That(model, Is.Not.Null);
            Assert.That(model.Id, Is.EqualTo(chapter.Id));
            Assert.That(model.Code, Is.EqualTo(chapter.Code));
            Assert.That(model.Description, Is.EqualTo(chapter.Description));
            Assert.That(model.ExerciseSummaries, Is.Not.Null);
            Assert.That(model.ExerciseSummaries.Count, Is.EqualTo(chapter.Assignments.Count));

            foreach (var assignment in chapter.Assignments)
            {
                var userAssignmentSummary = model.ExerciseSummaries.FirstOrDefault(summary => summary.AssignmentId == assignment.Id);
                Assert.That(userAssignmentSummary, Is.Not.Null);
                Assert.That(userAssignmentSummary.Code, Is.EqualTo(assignment.Code));
                Assert.That(userAssignmentSummary.NumberOfPassedTests, Is.EqualTo(numberOfPassingTests));
                Assert.That(userAssignmentSummary.NumberOfFailedTests, Is.EqualTo(numberOfFailingTests));
                Assert.That(userAssignmentSummary.NumberOfTests, Is.EqualTo(numberOfTests));
            }
        }

        [Test]
        public void ToChapterSummaryModel_ShouldThrowArgumentExceptionWhenAssignmentsAreMissing()
        {
            //Arrange
            var chapter = new ChapterBuilder().Build();
            chapter.Assignments = null;

            //Act + Assert
            Assert.That(() => _converter.ToChapterSummaryModel(chapter, new List<AssignmentResultDto>()), Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void ToChapterSummaryModel_ShouldThrowArgumentExceptionWhenTestsOfAssignmentsAreMissing()
        {
            //Arrange
            var chapter = new ChapterBuilder().WithAssignments(1, 1).Build();
            chapter.Assignments.First().Tests = null;

            //Act + Assert
            Assert.That(() => _converter.ToChapterSummaryModel(chapter, new List<AssignmentResultDto>()), Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void ToChapterModel_ShouldCorrectlyCovertValidChapter()
        {
            //Arrange
            var chapter = new ChapterBuilder().WithId().Build();

            //Act
            var model = _converter.ToChapterModel(chapter);

            //Assert
            Assert.That(model, Is.Not.Null);
            Assert.That(model.Id, Is.EqualTo(chapter.Id));
            Assert.That(model.Code, Is.EqualTo(chapter.Code));
            Assert.That(model.Description, Is.EqualTo(chapter.Description));
        }

        private IList<AssignmentResultDto> GenerateAssignmentResults(Chapter chapter,
            int numberOfPassingTests,
            int numberOfFailingTests)
        {
            var assignmentResults = new List<AssignmentResultDto>();
            foreach (var assignment in chapter.Assignments)
            {
                var assignmentResult = GenerateAssignmentResult(assignment, numberOfPassingTests, numberOfFailingTests);
                assignmentResults.Add(assignmentResult);
            }
            return assignmentResults;
        }

        private AssignmentResultDto GenerateAssignmentResult(Assignment assignment,
            int numberOfPassingTests,
            int numberOfFailingTests)
        {
            var assignmentResult = new AssignmentResultDto
            {
                AssignmentId = assignment.Id,
                TestResults = new List<TestResult>()
            };
            foreach (var test in assignment.Tests)
            {
                if (numberOfPassingTests <= 0 && numberOfFailingTests <= 0) continue;

                var passed = numberOfPassingTests > 0;
                assignmentResult.TestResults.Add(new TestResult
                {
                    TestId = test.Id,
                    Passed = passed
                });

                if (passed)
                {
                    numberOfPassingTests--;
                }
                else
                {
                    numberOfFailingTests--;
                }
            }
            return assignmentResult;
        }
    }

    [TestFixture]
    internal class ProjectConverterTests
    {
        private ProjectConverter _converter;

        [SetUp]
        public void Setup()
        {
            _converter = new ProjectConverter();
        }

        [Test]
        public void ToProjectModel_ShouldCorrectlyCovertValidProject()
        {
            //Arrange
            var project = new ProjectBuilder().WithId().Build();

            //Act
            var model = _converter.ToProjectModel(project);

            //Assert
            Assert.That(model, Is.Not.Null);
            Assert.That(model.Id, Is.EqualTo(project.Id));
            Assert.That(model.Code, Is.EqualTo(project.Code));
            Assert.That(model.Description, Is.EqualTo(project.Description));
        }
    }
}
