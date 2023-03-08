using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Api.Models.Converters;
using Guts.Business.Dtos;
using Guts.Business.Tests.Builders;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.TestRunAggregate;
using Guts.Domain.TopicAggregate.ChapterAggregate;
using NUnit.Framework;

namespace Guts.Api.Tests.Models.Converters
{
    [TestFixture]
    internal class TopicConverterTests
    {
        private TopicConverter _converter;

        [SetUp]
        public void Setup()
        {
            _converter = new TopicConverter();
        }

        [Test]
        [TestCase(5, 5, 0, 10)]
        [TestCase(5, 0, 5, 1)]
        [TestCase(5, 1, 1, 10)]
        public void ToTopicSummaryModel_ShouldCorrectlyConvertValidTopic(int numberOfTests,
            int numberOfPassingTests,
            int numberOfFailingTests,
            int numberOfUsers)
        {
            //Arrange
            var chapter = new ChapterBuilder().WithId().WithAssignments(5, numberOfTests).Build();
            var userAssignmentResults = GenerateAssignmentResults(chapter, numberOfPassingTests, numberOfFailingTests);

            //Act
            var model = _converter.ToTopicSummaryModel(chapter, userAssignmentResults);

            //Assert
            Assert.That(model, Is.Not.Null);
            Assert.That(model.Id, Is.EqualTo(chapter.Id));
            Assert.That(model.Code, Is.EqualTo(chapter.Code));
            Assert.That(model.Description, Is.EqualTo(chapter.Description));
            Assert.That(model.AssignmentSummaries, Is.Not.Null);
            Assert.That(model.AssignmentSummaries.Count, Is.EqualTo(chapter.Assignments.Count));

            foreach (var assignment in chapter.Assignments)
            {
                var userAssignmentSummary = model.AssignmentSummaries.FirstOrDefault(summary => summary.AssignmentId == assignment.Id);
                Assert.That(userAssignmentSummary, Is.Not.Null);
                Assert.That(userAssignmentSummary.Code, Is.EqualTo(assignment.Code));
                Assert.That(userAssignmentSummary.Description, Is.EqualTo(assignment.Description));
                Assert.That(userAssignmentSummary.NumberOfPassedTests, Is.EqualTo(numberOfPassingTests));
                Assert.That(userAssignmentSummary.NumberOfFailedTests, Is.EqualTo(numberOfFailingTests));
                Assert.That(userAssignmentSummary.NumberOfTests, Is.EqualTo(numberOfTests));
            }
        }

        [Test]
        [TestCase("1", "Assignment 1")]
        [TestCase("123", "Assignment 123")]
        [TestCase("a", "a")]
        [TestCase("abc", "abc")]
        public void ToTopicSummaryModel_ShouldCreateDescriptionsForAssignmentsIfTheyAreMissing(string assignmentCode, string expectedDescription)
        {
            //Arrange
            var chapter = new ChapterBuilder().WithId().WithAssignments(1, 2).Build();
            var assignment = chapter.Assignments.First();
            assignment.Code = assignmentCode;
            assignment.Description = null;
            var userAssignmentResults = GenerateAssignmentResults(chapter, 2, 2);

            //Act
            var model = _converter.ToTopicSummaryModel(chapter, userAssignmentResults);

            //Assert
            Assert.That(model, Is.Not.Null);
            Assert.That(model.AssignmentSummaries, Is.Not.Null);
            Assert.That(model.AssignmentSummaries.Count, Is.EqualTo(chapter.Assignments.Count));

            var userAssignmentSummary = model.AssignmentSummaries.FirstOrDefault(summary => summary.AssignmentId == assignment.Id);
            Assert.That(userAssignmentSummary, Is.Not.Null);
            Assert.That(userAssignmentSummary.Description, Is.EqualTo(expectedDescription));
        }

        [Test]
        public void ToTopicSummaryModel_ShouldThrowArgumentExceptionWhenAssignmentsAreMissing()
        {
            //Arrange
            var chapter = new ChapterBuilder().Build();
            chapter.Assignments = null;

            //Act + Assert
            Assert.That(() => _converter.ToTopicSummaryModel(chapter, new List<AssignmentResultDto>()), Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void ToTopicSummaryModel_ShouldThrowArgumentExceptionWhenTestsOfAssignmentsAreMissing()
        {
            //Arrange
            var chapter = new ChapterBuilder().WithAssignments(1, 1).Build();
            chapter.Assignments.First().Tests = null;

            //Act + Assert
            Assert.That(() => _converter.ToTopicSummaryModel(chapter, new List<AssignmentResultDto>()), Throws.InstanceOf<ArgumentException>());
        }

        private IReadOnlyList<AssignmentResultDto> GenerateAssignmentResults(Chapter chapter,
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
}