using System;
using System.Collections.Generic;
using Guts.Api.Models.Converters;
using Guts.Business;
using Guts.Business.Tests.Builders;
using Guts.Common.Extensions;
using Guts.Domain;
using NUnit.Framework;

namespace Guts.Api.Tests.Models.Converters
{
    [TestFixture]
    internal class ExerciseConverterTests
    {
        private ExerciseConverter _converter;
        private Random _random;


        [SetUp]
        public void Setup()
        {
            _converter = new ExerciseConverter();
            _random = new Random();
        }

        [Test]
        public void ToExerciseDetailModel_ShouldThrowArgumentExceptionIfChapterOfExerciseIsNotLoaded()
        {
            //Arrange
            var exercise = new ExerciseBuilder().WithoutChapterLoaded().Build();

            //Act + Assert
            Assert.That(
                () => _converter.ToExerciseDetailModel(exercise, new List<TestResult>(), new ExerciseTestRunInfoDto()),
                Throws.ArgumentException);
        }

        [Test]
        public void ToExerciseDetailModel_ShouldThrowArgumentExceptionIfCourseOfChapterOfExerciseIsNotLoaded()
        {
            //Arrange
            var chapter = new ChapterBuilder().WithoutCourseLoaded().Build();
            var exercise = new ExerciseBuilder().WithChapter(chapter).Build();

            //Act + Assert
            Assert.That(
                () => _converter.ToExerciseDetailModel(exercise, new List<TestResult>(), new ExerciseTestRunInfoDto()),
                Throws.ArgumentException);
        }

        [Test]
        public void ToExerciseDetailModel_ShouldThrowArgumentExceptionIfTestsAreNotLoaded()
        {
            //Arrange
            var exercise = new ExerciseBuilder().WithoutTestsLoaded().Build();

            //Act + Assert
            Assert.That(
                () => _converter.ToExerciseDetailModel(exercise, new List<TestResult>(), new ExerciseTestRunInfoDto()),
                Throws.ArgumentException);
        }

        [Test]
        public void ToExerciseDetailModel_ShouldThrowArgumentExceptionIfTestRunInfoIsNotProvided()
        {
            //Arrange
            var chapter = new ChapterBuilder().WithCourse().Build();
            var exercise = new ExerciseBuilder().WithChapter(chapter).Build();

            //Act + Assert
            Assert.That(
                () => _converter.ToExerciseDetailModel(exercise, new List<TestResult>(), null),
                Throws.ArgumentNullException);
        }

        [Test]
        public void ToExerciseDetailModel_ShouldAlsoWorkWhenNoTestResultsAreProvided()
        {
            //Arrange
            var chapter = new ChapterBuilder().WithCourse().Build();
            var exercise = new ExerciseBuilder().WithChapter(chapter).Build();

            //Act
            var model = _converter.ToExerciseDetailModel(exercise, null, new ExerciseTestRunInfoDto());

            //Assert
            Assert.That(model, Is.Not.Null);
            Assert.That(model.TestResults, Is.Not.Null);
            Assert.That(model.TestResults, Has.Count.Zero);
        }

        [Test]
        public void ToExerciseDetailModel_ShouldCorrectlyConvertTestRunInfoFields()
        {
            //Arrange
            var chapter = new ChapterBuilder().WithCourse().Build();
            var exercise = new ExerciseBuilder().WithChapter(chapter).Build();
            var utcNow = DateTime.UtcNow;

            var testRunInfo = new ExerciseTestRunInfoDto
            {
                FirstRunDateTime = utcNow.AddDays(-1),
                LastRunDateTime = utcNow,
                NumberOfRuns = _random.NextPositive()
            };

            //Act
            var model = _converter.ToExerciseDetailModel(exercise, new List<TestResult>(), testRunInfo);

            //Assert
            Assert.That(model, Is.Not.Null);
            Assert.That(model.FirstRun, Is.EqualTo(testRunInfo.FirstRunDateTime));
            Assert.That(model.LastRun, Is.EqualTo(testRunInfo.LastRunDateTime));
            Assert.That(model.NumberOfRuns, Is.EqualTo(testRunInfo.NumberOfRuns));
        }

        [Test]
        public void ToExerciseDetailModel_ShouldHaveATestResultForEachTest()
        {
            //Arrange
            var chapter = new ChapterBuilder().WithCourse().Build();
            var numberOfTests = _random.Next(2, 10);
            var exercise = new ExerciseBuilder().WithChapter(chapter).WithRandomTests(numberOfTests).Build();

            //Act
            var model = _converter.ToExerciseDetailModel(exercise, new List<TestResult>(), new ExerciseTestRunInfoDto());

            //Assert
            Assert.That(model, Is.Not.Null);
            Assert.That(model.TestResults, Is.Not.Null);
            Assert.That(model.TestResults, Has.Count.EqualTo(exercise.Tests.Count));
        }
    }
}