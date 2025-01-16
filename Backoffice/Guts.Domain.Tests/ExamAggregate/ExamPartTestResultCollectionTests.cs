using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Common.Extensions;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.ExamAggregate;
using Guts.Domain.TestRunAggregate;
using Guts.Domain.Tests.Builders;
using NUnit.Framework;

namespace Guts.Domain.Tests.ExamAggregate
{
    [TestFixture]
    public class ExamPartTestResultCollectionTests
    {
        [Test]
        public void FromLastTestResults_ShouldOrganizeTestResultsByUserAndAssignment()
        {
            //Arrange
            var user1Id = Random.Shared.NextPositive();
            var user2Id = Random.Shared.NextPositive();

            var assignment1 = new AssignmentBuilder().WithId().WithRandomTests(2).Build();
            var assignment2 = new AssignmentBuilder().WithId().WithRandomTests(1).Build();

            var lastTestResults = new List<TestResult>
            {
                //user1
                new TestResultBuilder().WithUser(user1Id).WithPassed(true).WithTest(assignment1.Tests.First()).Build(),
                new TestResultBuilder().WithUser(user1Id).WithPassed(true).WithTest(assignment1.Tests.ElementAt(1)).Build(),
                new TestResultBuilder().WithUser(user1Id).WithPassed(true).WithTest(assignment2.Tests.First()).Build(),

                //user2
                new TestResultBuilder().WithUser(user2Id).WithPassed(false).WithTest(assignment1.Tests.First()).Build(),
                new TestResultBuilder().WithUser(user2Id).WithPassed(false).WithTest(assignment1.Tests.ElementAt(1)).Build(),
                new TestResultBuilder().WithUser(user2Id).WithPassed(false).WithTest(assignment2.Tests.First()).Build(),
            };

            //Act
            var collection = ExamPartTestResultCollection.FromLastTestResults(lastTestResults);

            var assignment1ResultForUser1 = collection.GetAssignmentResultFor(user1Id, assignment1.Id);
            var assignment2ResultForUser1 = collection.GetAssignmentResultFor(user1Id, assignment2.Id);
            var assignment1ResultForUser2 = collection.GetAssignmentResultFor(user2Id, assignment1.Id);
            var assignment2ResultForUser2 = collection.GetAssignmentResultFor(user2Id, assignment2.Id);

            //Assert
            AssertAssignmentResult(assignment1ResultForUser1, user1Id, assignment1.Id, 2);
            AssertAssignmentResult(assignment2ResultForUser1, user1Id, assignment2.Id, 1);
            AssertAssignmentResult(assignment1ResultForUser2, user2Id, assignment1.Id, 0);
            AssertAssignmentResult(assignment2ResultForUser2, user2Id, assignment2.Id, 0);
        }

        private static void AssertAssignmentResult(IAssignmentResult assignment1ResultForUser1, int expectedUserId,
            int expectedAssignmentId, int expectedNumberOfPassingTests)
        {
            Assert.That(assignment1ResultForUser1.UserId, Is.EqualTo(expectedUserId));
            Assert.That(assignment1ResultForUser1.AssignmentId, Is.EqualTo(expectedAssignmentId));
            Assert.That(assignment1ResultForUser1.NumberOfPassingTests, Is.EqualTo(expectedNumberOfPassingTests));
        }
    }
}