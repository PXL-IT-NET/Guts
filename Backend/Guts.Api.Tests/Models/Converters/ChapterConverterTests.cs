using System;
using System.Linq;
using Guts.Api.Models.Converters;
using Guts.Business.Tests.Builders;
using NUnit.Framework;

namespace Guts.Api.Tests.Models.Converters
{

    [TestFixture]
    internal class ChapterConverterTests
    {
        private ChapterConverter _converter;
        private readonly Random _random;
       

        public ChapterConverterTests()
        {
            _random = new Random();
        }

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

            //Act
            var model = _converter.ToChapterContentsModel(chapter);

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
        public void ToChapterContentsModel_ShouldThrowArgumentExceptionWhenExercisesAreMissing()
        {
            //Arrange
            var chapter = new ChapterBuilder().Build();
            chapter.Exercises = null;

            //Act + Assert
            Assert.That(() => _converter.ToChapterContentsModel(chapter), Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void ToChapterContentsModel_ShouldThrowArgumentExceptionWhenTestsOfExercisesAreMissing()
        {
            //Arrange
            var chapter = new ChapterBuilder().WithExercises(1,1).Build();
            chapter.Exercises.First().Tests = null;

            //Act + Assert
            Assert.That(() => _converter.ToChapterContentsModel(chapter), Throws.InstanceOf<ArgumentException>());
        }
    }
}
