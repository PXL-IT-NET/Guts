using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Business.Converters;
using Guts.Business.Tests.Builders;
using Guts.Common.Extensions;
using Guts.Domain.TestRunAggregate;
using NUnit.Framework;

namespace Guts.Business.Tests.Converters
{
    [TestFixture]
    public class AssignmentWitResultsConverterTests
    {
        [Test]
        public void ToAssignmentStatisticsDtoShouldCorrectlyGenerateStatistics()
        {
            //Arrange
            var converter = new AssignmentWitResultsConverter();
   
            var random = new Random();
            var assigmentId = random.NextPositive();
            var user1Id = random.NextPositive();
            var user2Id = random.NextPositive();
            var user3Id = random.NextPositive();
            var test1Id = random.NextPositive();
            var test2Id = random.NextPositive();

            //create test results
            //user 1 and user 2 both have one passing test
            //user 3 passes no tests
            var testResults = new List<TestResult>
            {
                new TestResultBuilder().WithId().WithTest(test1Id).WithUser(user1Id).WithPassed(false).Build(),
                new TestResultBuilder().WithId().WithTest(test1Id).WithUser(user2Id).WithPassed(true).Build(),
                new TestResultBuilder().WithId().WithTest(test1Id).WithUser(user3Id).WithPassed(false).Build(),
                new TestResultBuilder().WithId().WithTest(test2Id).WithUser(user1Id).WithPassed(true).Build(),
                new TestResultBuilder().WithId().WithTest(test2Id).WithUser(user2Id).WithPassed(false).Build(),
                new TestResultBuilder().WithId().WithTest(test2Id).WithUser(user3Id).WithPassed(false).Build(),
            };

            //Act
            var result = converter.ToAssignmentStatisticsDto(assigmentId, testResults);

            //Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.AssignmentId, Is.EqualTo(assigmentId));
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
