using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Business.Converters;
using Guts.Business.Tests.Builders;
using Guts.Common.Extensions;
using Guts.Data;
using NUnit.Framework;

namespace Guts.Business.Tests.Converters
{
    [TestFixture]
    public class TestResultConverterTests
    {
        [Test]
        public void ToExerciseResultDtoShouldGroupExercises()
        {
            //Arrange
            var converter = new TestResultConverter();
            var random = new Random();
            var exercise1Id = random.NextPositive();
            var exercise2Id = random.NextPositive();
            var testsWithLastUserResults = new List<TestWithLastUserResults>
            {
                new TestWithLastUserResultsBuilder().WithExerciseId(exercise1Id).WithUserResults(3).Build(),
                new TestWithLastUserResultsBuilder().WithExerciseId(exercise2Id).WithUserResults(3).Build(),
                new TestWithLastUserResultsBuilder().WithExerciseId(exercise1Id).WithUserResults(3).Build(),
                new TestWithLastUserResultsBuilder().WithExerciseId(exercise2Id).WithUserResults(3).Build()
            };

            //Act
            var results = converter.ToExerciseResultDto(testsWithLastUserResults);

            //Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results, Has.One.With.Matches((ExerciseResultDto result) => result.ExerciseId == exercise1Id));
            Assert.That(results, Has.One.With.Matches((ExerciseResultDto result) => result.ExerciseId == exercise2Id));
        }

        [Test]
        public void ToExerciseResultDtoShouldConvertTestResults()
        {
            //Arrange
            var converter = new TestResultConverter();
            var numberOfUsers = 5;
            var exerciseId = new Random().NextPositive();
            var testsWithLastUserResults = new List<TestWithLastUserResults>
            {
                new TestWithLastUserResultsBuilder().WithExerciseId(exerciseId).WithUserResults(numberOfUsers).Build(),
                new TestWithLastUserResultsBuilder().WithExerciseId(exerciseId).WithUserResults(numberOfUsers).Build()
            };

            //Act
            var results = converter.ToExerciseResultDto(testsWithLastUserResults);

            //Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count, Is.EqualTo(1)); //one exercise, so one result is expected

            var exerciseResultDto = results.First();
            Assert.That(exerciseResultDto.TestResults.Count, Is.EqualTo(testsWithLastUserResults.Count));

            //TODO: test if averages are calculated correctly

            foreach (var testWithUserResults in testsWithLastUserResults)
            {
                Assert.That(exerciseResultDto.TestResults, Has.Some.Matches((TestResultDto dto) => dto.TestName == testWithUserResults.Test.TestName));
            }
        }
    }
}
