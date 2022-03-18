using System.Collections.Generic;
using System.Dynamic;
using Guts.Common.Extensions;
using Guts.Domain.ExamAggregate;
using Guts.Domain.Tests.Builders;
using Moq;
using NUnit.Framework;

namespace Guts.Domain.Tests.ExamAggregate
{
    [TestFixture]
    public class ExamScoreTests : DomainTestBase
    {
        [Test]
        public void Constructor_ShouldSetPropertiesUsingUserAndExam()
        {
            //Arrange
            var user = new UserBuilder().Build();
            var examBuilder = new ExamBuilder();
            double maximumScore = 0.0;
            for (int i = 0; i < Random.Next(2,11); i++)
            {
                var examPartMock = new Mock<IExamPart>();
                var examPartMaximumScore = Random.Next(10, 100);
                maximumScore += examPartMaximumScore;
                examPartMock.SetupGet(part => part.MaximumScore).Returns(examPartMaximumScore);
                examBuilder.WithExamPart(examPartMock.Object);
            }

            var exam = examBuilder.Build();

            //Act
            var examScore = new ExamScore(user, exam);

            //Assert
            Assert.That(examScore.FirstName, Is.EqualTo(user.FirstName));
            Assert.That(examScore.LastName, Is.EqualTo(user.LastName));
            Assert.That(examScore.NormalizedMaximumScore, Is.EqualTo(exam.MaximumScore));
            Assert.That(examScore.MaximumScore, Is.EqualTo(maximumScore));
            Assert.That(examScore.Score, Is.Zero);
            Assert.That(examScore.ExamPartScores, Has.Count.Zero);
        }

        [Test]
        public void Constructor_ShouldTrimUserFirstAndLastName()
        {
            //Arrange
            var user = new UserBuilder()
                .WithFirstName("   John Jane ")
                .WithLastName("  Doe the unknown    ")
                .Build();
            var exam = new ExamBuilder().Build();

            //Act
            var examScore = new ExamScore(user, exam);

            //Assert
            Assert.That(examScore.FirstName, Is.EqualTo("John Jane"));
            Assert.That(examScore.LastName, Is.EqualTo("Doe the unknown"));
        }

        [Test]
        public void ToCsvRecord_ShouldContainTheUserDataAndScoreTotals()
        {
            //Arrange
            var user = new UserBuilder().Build();
            var exam = new ExamBuilder().Build();
            var examScore = new ExamScore(user, exam);

            //Act
            ExpandoObject record = examScore.ToCsvRecord();

            //Assert
            var dictionary = (IDictionary<string, object>) record;
            Assert.That(dictionary["LastName"], Is.EqualTo(examScore.LastName));
            Assert.That(dictionary["FirstName"], Is.EqualTo(examScore.FirstName));

            Assert.That(dictionary[$"Total({examScore.MaximumScore})"], Is.EqualTo(examScore.Score));
            Assert.That(dictionary[$"Total({examScore.NormalizedMaximumScore})"], Is.EqualTo(examScore.NormalizedScore));
        }

        [Test]
        public void ToCsvRecord_ShouldContainScoresOfEachAssignmentOfEachExamPart()
        {
            //Arrange
            var user = new UserBuilder().Build();
            var exam = new ExamBuilder().Build();
            var examScore = new ExamScore(user, exam);
            AddSomeExamPartScores(examScore);

            //Act
            ExpandoObject record = examScore.ToCsvRecord();

            //Assert
            var dictionary = (IDictionary<string, object>)record;
            foreach (var examPartScore in examScore.ExamPartScores)
            {
                Assert.That(dictionary[$"Total_{examPartScore.ExamPartDescription}({examPartScore.MaximumScore})"], Is.EqualTo(examPartScore.Score));
                foreach (var assignmentEvaluationScore in examPartScore.AssignmentEvaluationScores)
                {
                    Assert.That(dictionary[$"{assignmentEvaluationScore.AssignmentDescription}_NbrPassed({assignmentEvaluationScore.NumberOfTests})"], 
                        Is.EqualTo(assignmentEvaluationScore.NumberOfPassedTests));
                    Assert.That(dictionary[$"{assignmentEvaluationScore.AssignmentDescription}_Score({assignmentEvaluationScore.MaximumScore})"],
                        Is.EqualTo(assignmentEvaluationScore.Score));
                }
            }
        }

        private void AddSomeExamPartScores(ExamScore examScore)
        {
            for (int i = 0; i < Random.Next(1, 5); i++)
            {
                List<IAssignmentEvaluationScore> assignmentEvaluationScores = new List<IAssignmentEvaluationScore>();
                for (int j = 0; j < Random.Next(1, 5); j++)
                {
                    var assignmentEvaluationScoreMock = new Mock<IAssignmentEvaluationScore>();
                    assignmentEvaluationScoreMock.SetupGet(score => score.AssignmentDescription)
                        .Returns(Random.NextString());
                    assignmentEvaluationScoreMock.SetupGet(score => score.NumberOfTests)
                        .Returns(Random.Next(5, 11));
                    assignmentEvaluationScoreMock.SetupGet(score => score.NumberOfPassedTests)
                        .Returns(Random.Next(0, 6));
                    assignmentEvaluationScoreMock.SetupGet(score => score.MaximumScore)
                        .Returns(Random.Next(10, 21));
                    assignmentEvaluationScoreMock.SetupGet(score => score.Score)
                        .Returns(Random.Next(0, 11));
                    assignmentEvaluationScores.Add(assignmentEvaluationScoreMock.Object);
                }

                var examPartScoreMock = new Mock<IExamPartScore>();
                examPartScoreMock.SetupGet(examPartScore => examPartScore.AssignmentEvaluationScores)
                    .Returns(assignmentEvaluationScores);
                examPartScoreMock.SetupGet(examPartScore => examPartScore.ExamPartDescription)
                    .Returns(Random.NextString());
                examPartScoreMock.SetupGet(examPartScore => examPartScore.MaximumScore)
                    .Returns(Random.Next(10, 21));
                examPartScoreMock.SetupGet(examPartScore => examPartScore.Score)
                    .Returns(Random.Next(0, 11));

                examScore.AddExamPartScore(examPartScoreMock.Object);
            }
        }
    }
}