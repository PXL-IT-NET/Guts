using Guts.Domain.ExamAggregate;
using Guts.Domain.Tests.Builders;
using NUnit.Framework;

namespace Guts.Domain.Tests.ExamAggregate
{
    [TestFixture]
    public class AssignmentEvaluationScoreTests : DomainTestBase
    {
        [Test]
        public void Constructor_ShouldCopyPropertiesFromAssignmentEvaluation()
        {
            //Arrange
            var assignmentEvaluation = BuildRandomAssignmentEvaluation();

            //Act
            var evaluationScore = new AssignmentEvaluationScore(assignmentEvaluation);

            //Assert
            Assert.That(evaluationScore.AssignmentEvaluationId, Is.EqualTo(assignmentEvaluation.Id));
            Assert.That(evaluationScore.AssignmentDescription, Is.EqualTo(assignmentEvaluation.Assignment.Description));
            Assert.That(evaluationScore.MaximumScore, Is.EqualTo(assignmentEvaluation.MaximumScore));
            Assert.That(evaluationScore.NumberOfTests, Is.EqualTo(assignmentEvaluation.Assignment.Tests.Count));
            Assert.That(evaluationScore.NumberOfPassedTests, Is.Zero);
            Assert.That(evaluationScore.Score, Is.Zero);
        }

        [Test]
        [TestCase(10,10,0, 10, 10)]
        [TestCase(10, 5, 0, 5, 10)]
        [TestCase(10, 5, 4, 4, 0)]
        [TestCase(10, 5, 4, 2, 0)]
        [TestCase(20, 10, 5, 8, 12)]
        public void Score_ShouldBeCalculatedCorrectly(int maximumScore,int numberOfTests, int numberOfTestsAlreadyGreen, int numberOfPassingTests, double expectedScore)
        {
            var assignmentEvaluation = BuildAssignmentEvaluation(maximumScore, numberOfTests, numberOfTestsAlreadyGreen);

            var evaluationScore = new AssignmentEvaluationScore(assignmentEvaluation)
            {
                NumberOfPassedTests = numberOfPassingTests
            };

            Assert.That(evaluationScore.Score, Is.EqualTo(expectedScore));
        }

        [Test]
        public void EqualityOperator_ShouldReturnTrueWhenAssignmentEvaluationIdAndNumberOfPassingTestsAreTheSame()
        {
            //Arrange
            var assignmentEvaluation = BuildRandomAssignmentEvaluation();
            var score1 = new AssignmentEvaluationScore(assignmentEvaluation);
            var score2 = new AssignmentEvaluationScore(assignmentEvaluation);
            var numberOfPassedTests = Random.Next(0, score1.NumberOfTests + 1);
            score1.NumberOfPassedTests = numberOfPassedTests;
            score2.NumberOfPassedTests = numberOfPassedTests;

            //Act + Assert
            Assert.That(score1 == score2, Is.True);
        }

        private AssignmentEvaluation BuildRandomAssignmentEvaluation()
        {
            return BuildAssignmentEvaluation(Random.Next(10, 101), Random.Next(5, 21), 0);
        }

        private AssignmentEvaluation BuildAssignmentEvaluation(int maximumScore, int numberOfTests, int numberOfTestsAlreadyGreen)
        {
            var assignment = new AssignmentBuilder()
                .WithId()
                .WithRandomTests(numberOfTests)
                .Build();
            var assignmentEvaluation = new AssignmentEvaluationBuilder()
                .WithId()
                .WithAssignment(assignment)
                .WithMaximumScore(maximumScore)
                .WithNumberOfTestsAlreadyGreenAtStart(numberOfTestsAlreadyGreen)
                .Build();
            return assignmentEvaluation;
        }
    }
}