using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Business.Converters;
using Guts.Business.Tests.Builders;
using Guts.Common.Extensions;
using Guts.Data;
using Guts.Domain;
using NUnit.Framework;

namespace Guts.Business.Tests.Converters
{
    [TestFixture]
    public class AssignmentWitResultsConverterTests
    {
        [Test]
        public void ToAssignmentResultDtoShouldConvertTestResults()
        {
            //Arrange
            var converter = new AssignmentWitResultsConverter();
            var assigment = new ExerciseBuilder().Build();
            var testsWithResults = new List<TestWithLastResultOfUser>
            {
                new TestWithLastResultOfUserBuilder().WithAssignmentId(assigment.Id).Build(),
                new TestWithLastResultOfUserBuilder().WithAssignmentId(assigment.Id).Build(),
            };
            var assignmentWithResults = new AssignmentWithLastResultsOfUser
            {
                Assignment = assigment,
                TestsWithLastResultOfUser = testsWithResults
            };

            //Act
            var result = converter.ToAssignmentResultDto(assignmentWithResults);

            //Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TestResults.Count, Is.EqualTo(testsWithResults.Count));

            foreach (var testWithUserResults in testsWithResults)
            {
                Assert.That(result.TestResults, Has.Some.Matches((TestResultDto dto) => dto.TestName == testWithUserResults.Test.TestName));
            }
        }

        [Test]
        public void ToAssignmentStatisticsDtoShouldCorrectlyGenerateStatistics()
        {
            //Arrange
            var converter = new AssignmentWitResultsConverter();
            var assigment = new ExerciseBuilder().Build();
            var random = new Random();
            var user1Id = random.NextPositive();
            var user2Id = random.NextPositive();
            var user3Id = random.NextPositive();

            //create test results
            //user 1 and user 2 both have one passing test
            //user 3 passes no tests
            var testsWithResultsOfMultipleUsers = new List<TestWithLastResultOfMultipleUsers>
            {
                new TestWithLastResultOfMultipleUsersBuilder()
                    .WithAssignmentId(assigment.Id)
                    .WithUserResult(user1Id, false)
                    .WithUserResult(user2Id, true)
                    .WithUserResult(user3Id, false)
                    .Build(),
                new TestWithLastResultOfMultipleUsersBuilder()
                    .WithAssignmentId(assigment.Id)
                    .WithUserResult(user1Id, true)
                    .WithUserResult(user2Id, false)
                    .WithUserResult(user3Id, false)
                    .Build(),
            };
            var assignmentWithResultsOfMultipleUsers = new AssignmentWithLastResultsOfMultipleUsers()
            {
                Assignment = assigment,
                TestsWithLastResultOfMultipleUsers = testsWithResultsOfMultipleUsers
            };

            //Act
            var result = converter.ToAssignmentStatisticsDto(assignmentWithResultsOfMultipleUsers);

            //Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.AssignmentId, Is.EqualTo(assigment.Id));
            Assert.That(result.TestPassageStatistics.Count, Is.EqualTo(2));

            var firstStatistics = result.TestPassageStatistics.First();
            Assert.That(firstStatistics.AmountOfPassedTests, Is.EqualTo(0));
            Assert.That(firstStatistics.AmountOfUsers, Is.EqualTo(1)); //user 3

            var secondStatistics = result.TestPassageStatistics.ElementAt(1);
            Assert.That(secondStatistics.AmountOfPassedTests, Is.EqualTo(1));
            Assert.That(secondStatistics.AmountOfUsers, Is.EqualTo(2)); //user 1 and user 2
        }

    }
}
