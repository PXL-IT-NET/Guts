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
            var chapter = new ChapterBuilder().WithId().WithExercises(5, numberOfTests).Build();
            var userExerciseResults = GenerateAssignmentResults(chapter, numberOfPassingTests, numberOfFailingTests);

            //Act
            var model = _converter.ToChapterSummaryModel(chapter, userExerciseResults);

            //Assert
            Assert.That(model, Is.Not.Null);
            Assert.That(model.Id, Is.EqualTo(chapter.Id));
            Assert.That(model.Number, Is.EqualTo(chapter.Number));
            Assert.That(model.ExerciseSummaries, Is.Not.Null);
            Assert.That(model.ExerciseSummaries.Count, Is.EqualTo(chapter.Exercises.Count));

            foreach (var exercise in chapter.Exercises)
            {
                var userExerciseSummary = model.ExerciseSummaries.FirstOrDefault(summary => summary.ExerciseId == exercise.Id);
                Assert.That(userExerciseSummary, Is.Not.Null);
                Assert.That(userExerciseSummary.Code, Is.EqualTo(exercise.Code));
                Assert.That(userExerciseSummary.NumberOfPassedTests, Is.EqualTo(numberOfPassingTests));
                Assert.That(userExerciseSummary.NumberOfFailedTests, Is.EqualTo(numberOfFailingTests));
                Assert.That(userExerciseSummary.NumberOfTests, Is.EqualTo(numberOfTests));
            }
        }

        [Test]
        public void ToChapterSummaryModel_ShouldThrowArgumentExceptionWhenExercisesAreMissing()
        {
            //Arrange
            var chapter = new ChapterBuilder().Build();
            chapter.Exercises = null;
            var userExerciseResults = new List<AssignmentResultDto>();
            //var averageExerciseResults = new List<AssignmentResultDto>();

            //Act + Assert
            Assert.That(() => _converter.ToChapterSummaryModel(chapter, userExerciseResults), Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void ToChapterSummaryModel_ShouldThrowArgumentExceptionWhenTestsOfExercisesAreMissing()
        {
            //Arrange
            var chapter = new ChapterBuilder().WithExercises(1, 1).Build();
            chapter.Exercises.First().Tests = null;
            var userExerciseResults = new List<AssignmentResultDto>();
       //     var averageExerciseResults = new List<AssignmentResultDto>();

            //Act + Assert
            Assert.That(() => _converter.ToChapterSummaryModel(chapter, userExerciseResults), Throws.InstanceOf<ArgumentException>());
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
            Assert.That(model.Number, Is.EqualTo(chapter.Number));
        }

        private IList<AssignmentResultDto> GenerateAssignmentResults(Chapter chapter,
            int numberOfPassingTests,
            int numberOfFailingTests)
        {
            var exerciseResults = new List<AssignmentResultDto>();
            foreach (var exercise in chapter.Exercises)
            {
                var exerciseResult = GenerateAssignmentResult(exercise, numberOfPassingTests, numberOfFailingTests);
                exerciseResults.Add(exerciseResult);
            }
            return exerciseResults;
        }

        private AssignmentResultDto GenerateAssignmentResult(Exercise exercise,
            int numberOfPassingTests,
            int numberOfFailingTests)
        {
            var exerciseResult = new AssignmentResultDto
            {
                AssignmentId = exercise.Id,
                TestResults = new List<TestResult>()
            };
            foreach (var test in exercise.Tests)
            {
                if (numberOfPassingTests <= 0 && numberOfFailingTests <= 0) continue;

                var passed = numberOfPassingTests > 0;
                exerciseResult.TestResults.Add(new TestResult
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
            return exerciseResult;
        }
    }
}
