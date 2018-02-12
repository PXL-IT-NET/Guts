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
        public void ToChapterContentsModel_ShouldCorrectlyConvertValidChapter()
        {
            //Arrange
            var chapter = new ChapterBuilder().WithId().WithExercises(5, 5).Build();
            var exerciseResults = new List<ExerciseResultDto>();

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
                Assert.That(exerciseSummary.NumberOfTests, Is.EqualTo(exercise.Tests.Count));
            }
        }

        [Test]
        public void ToChapterContentsModel_ShouldSetNumberOfPassedTestsToCountOfTestsIfAllTestsPassed()
        {
            //Arrange
            var chapter = new ChapterBuilder().WithId().WithExercises(5, 5).Build();
            var exerciseResults = GenerateExerciseResults(chapter, true);

            //Act
            var model = _converter.ToChapterContentsModel(chapter, exerciseResults);

            //Assert
            Assert.That(model, Is.Not.Null);
            Assert.That(model.Exercises, Is.Not.Null);

            foreach (var exercise in chapter.Exercises)
            {
                var exerciseSummary = model.Exercises.FirstOrDefault(summary => summary.ExerciseId == exercise.Id);
                Assert.That(exerciseSummary, Is.Not.Null);
                Assert.That(exerciseSummary.NumberOfPassedTests, Is.EqualTo(exercise.Tests.Count));
            }
        }

        [Test]
        public void ToChapterContentsModel_ShouldSetNumberOfPassedTestsToZeroIfAllTestsFailed()
        {
            //Arrange
            var chapter = new ChapterBuilder().WithId().WithExercises(5, 5).Build();
            var exerciseResults = GenerateExerciseResults(chapter, false);

            //Act
            var model = _converter.ToChapterContentsModel(chapter, exerciseResults);

            //Assert
            Assert.That(model, Is.Not.Null);
            Assert.That(model.Exercises, Is.Not.Null);

            foreach (var exercise in chapter.Exercises)
            {
                var exerciseSummary = model.Exercises.FirstOrDefault(summary => summary.ExerciseId == exercise.Id);
                Assert.That(exerciseSummary, Is.Not.Null);
                Assert.That(exerciseSummary.NumberOfPassedTests, Is.EqualTo(0));
            }
        }

        private IList<ExerciseResultDto> GenerateExerciseResults(Chapter chapter, bool testOutComeForEachTest)
        {
            var exerciseResults = new List<ExerciseResultDto>();
            foreach (var exercise in chapter.Exercises)
            {
                var exerciseResult = new ExerciseResultDto {ExerciseId = exercise.Id, TestResults = new List<TestResultDto>()};
                foreach (var test in exercise.Tests)
                {
                    exerciseResult.TestResults.Add(new TestResultDto
                    {
                        TestName = test.TestName,
                        Passed = testOutComeForEachTest
                    });
                }
                exerciseResults.Add(exerciseResult);
            }
            return exerciseResults;
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
    }
}
