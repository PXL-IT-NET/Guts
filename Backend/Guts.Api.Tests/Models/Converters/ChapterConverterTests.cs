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
        [TestCase(5, 5, 0, 10)]
        [TestCase(5, 0, 5, 1)]
        [TestCase(5, 1, 1, 10)]
        public void ToChapterContentsModel_ShouldCorrectlyConvertValidChapter(int numberOfTests, 
            int numberOfPassingTests, 
            int numberOfFailingTests,
            int numberOfUsers)
        {
            //Arrange
            var chapter = new ChapterBuilder().WithId().WithExercises(5, numberOfTests).Build();
            var userExerciseResults = GenerateExerciseResults(chapter, numberOfPassingTests, numberOfFailingTests, 1);
            var averageExerciseResults = GenerateExerciseResults(chapter, numberOfTests, 0, numberOfUsers);

            //Act
            var model = _converter.ToChapterContentsModel(chapter, userExerciseResults, averageExerciseResults);

            //Assert
            Assert.That(model, Is.Not.Null);
            Assert.That(model.Id, Is.EqualTo(chapter.Id));
            Assert.That(model.Number, Is.EqualTo(chapter.Number));
            Assert.That(model.UserExerciseSummaries, Is.Not.Null);
            Assert.That(model.UserExerciseSummaries.Count, Is.EqualTo(chapter.Exercises.Count));

            foreach (var exercise in chapter.Exercises)
            {
                var userExerciseSummary = model.UserExerciseSummaries.FirstOrDefault(summary => summary.ExerciseId == exercise.Id);
                Assert.That(userExerciseSummary, Is.Not.Null);
                Assert.That(userExerciseSummary.Number, Is.EqualTo(exercise.Number));
                Assert.That(userExerciseSummary.NumberOfPassedTests, Is.EqualTo(numberOfPassingTests));
                Assert.That(userExerciseSummary.NumberOfFailedTests, Is.EqualTo(numberOfFailingTests));
                Assert.That(userExerciseSummary.NumberOfTests, Is.EqualTo(numberOfTests));
                Assert.That(userExerciseSummary.NumberOfUsers, Is.EqualTo(1));

                var averageExerciseSummary = model.AverageExerciseSummaries.FirstOrDefault(summary => summary.ExerciseId == exercise.Id);
                Assert.That(averageExerciseSummary, Is.Not.Null);
                Assert.That(averageExerciseSummary.Number, Is.EqualTo(exercise.Number));
                Assert.That(averageExerciseSummary.NumberOfPassedTests, Is.EqualTo(numberOfTests));
                Assert.That(averageExerciseSummary.NumberOfFailedTests, Is.EqualTo(0));
                Assert.That(averageExerciseSummary.NumberOfTests, Is.EqualTo(numberOfTests));
                Assert.That(averageExerciseSummary.NumberOfUsers, Is.EqualTo(numberOfUsers));
            }
        }

        [Test]
        public void ToChapterContentsModel_ShouldThrowArgumentExceptionWhenExercisesAreMissing()
        {
            //Arrange
            var chapter = new ChapterBuilder().Build();
            chapter.Exercises = null;
            var userExerciseResults = new List<ExerciseResultDto>();
            var averageExerciseResults = new List<ExerciseResultDto>();

            //Act + Assert
            Assert.That(() => _converter.ToChapterContentsModel(chapter, userExerciseResults, averageExerciseResults), Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void ToChapterContentsModel_ShouldThrowArgumentExceptionWhenTestsOfExercisesAreMissing()
        {
            //Arrange
            var chapter = new ChapterBuilder().WithExercises(1, 1).Build();
            chapter.Exercises.First().Tests = null;
            var userExerciseResults = new List<ExerciseResultDto>();
            var averageExerciseResults = new List<ExerciseResultDto>();

            //Act + Assert
            Assert.That(() => _converter.ToChapterContentsModel(chapter, userExerciseResults, averageExerciseResults), Throws.InstanceOf<ArgumentException>());
        }

        private IList<ExerciseResultDto> GenerateExerciseResults(Chapter chapter, 
            int numberOfPassingTests, 
            int numberOfFailingTests,
            int numberOfUsers)
        {
            var exerciseResults = new List<ExerciseResultDto>();
            foreach (var exercise in chapter.Exercises)
            {
                var exerciseResult = GenerateExerciseResult(exercise, numberOfPassingTests, numberOfFailingTests, numberOfUsers);
                exerciseResults.Add(exerciseResult);
            }
            return exerciseResults;
        }

        private ExerciseResultDto GenerateExerciseResult(Exercise exercise, 
            int numberOfPassingTests, 
            int numberOfFailingTests,
            int numberOfUsers)
        {
            var exerciseResult = new ExerciseResultDto
            {
                ExerciseId = exercise.Id,
                UserCount = numberOfUsers,
                TestResults = new List<TestResultDto>()
            };
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
