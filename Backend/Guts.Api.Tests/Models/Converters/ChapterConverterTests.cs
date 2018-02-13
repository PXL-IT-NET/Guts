using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Api.Models.Converters;
using Guts.Business;
using Guts.Business.Tests.Builders;
using Guts.Domain;
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
            _converter = new ChapterConverter();
        }

        [Test]
        [TestCase(5, 5, 0)]
        [TestCase(5, 0, 5)]
        [TestCase(5, 1, 1)]
        public void ToChapterContentsModel_ShouldCorrectlyConvertValidChapter(int numberOfTests, int numberOfPassingTests, int numberOfFailingTests)
        {
            //Arrange
            var chapter = new ChapterBuilder().WithId().WithExercises(5, numberOfTests).Build();
            var exerciseResults = GenerateExerciseResults(chapter, numberOfPassingTests, numberOfFailingTests);

            //Act
            var model = _converter.ToChapterContentsModel(chapter, exerciseResults);

            //Assert
            Assert.That(model, Is.Not.Null);
            Assert.That(model.Exercises, Is.Not.Null);
            Assert.That(model.Exercises.Count, Is.EqualTo(chapter.Exercises.Count));

            foreach (var exercise in chapter.Exercises)
            {
                var exerciseSummary = model.Exercises.FirstOrDefault(summary => summary.ExerciseId == exercise.Id);
                Assert.That(exerciseSummary, Is.Not.Null);
                Assert.That(exerciseSummary.Number, Is.EqualTo(exercise.Number));
                Assert.That(exerciseSummary.NumberOfPassedTests, Is.EqualTo(numberOfPassingTests));
                Assert.That(exerciseSummary.NumberOfFailedTests, Is.EqualTo(numberOfFailingTests));
                Assert.That(exerciseSummary.NumberOfTests, Is.EqualTo(numberOfTests));
            }
        }

        [Test]
        public void ToChapterContentsModel_ShouldThrowArgumentExceptionWhenExercisesAreMissing()
        {
            //Arrange
            var chapter = new ChapterBuilder().Build();
            chapter.Exercises = null;
            var exerciseResults = new List<ExerciseResultDto>();

            //Act + Assert
            Assert.That(() => _converter.ToChapterContentsModel(chapter, exerciseResults), Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void ToChapterContentsModel_ShouldThrowArgumentExceptionWhenTestsOfExercisesAreMissing()
        {
            //Arrange
            var chapter = new ChapterBuilder().WithExercises(1, 1).Build();
            chapter.Exercises.First().Tests = null;
            var exerciseResults = new List<ExerciseResultDto>();

            //Act + Assert
            Assert.That(() => _converter.ToChapterContentsModel(chapter, exerciseResults), Throws.InstanceOf<ArgumentException>());
        }

        private IList<ExerciseResultDto> GenerateExerciseResults(Chapter chapter, int numberOfPassingTests, int numberOfFailingTests)
        {
            var exerciseResults = new List<ExerciseResultDto>();
            foreach (var exercise in chapter.Exercises)
            {
                var exerciseResult = GenerateExerciseResult(exercise, numberOfPassingTests, numberOfFailingTests);
                exerciseResults.Add(exerciseResult);
            }
            return exerciseResults;
        }

        private ExerciseResultDto GenerateExerciseResult(Exercise exercise, int numberOfPassingTests, int numberOfFailingTests)
        {
            var exerciseResult = new ExerciseResultDto { ExerciseId = exercise.Id, TestResults = new List<TestResultDto>() };
            foreach (var test in exercise.Tests)
            {
                if (numberOfPassingTests <= 0 && numberOfFailingTests <= 0) continue;

                var passed = numberOfPassingTests > 0;
                exerciseResult.TestResults.Add(new TestResultDto
                {
                    TestName = test.TestName,
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
