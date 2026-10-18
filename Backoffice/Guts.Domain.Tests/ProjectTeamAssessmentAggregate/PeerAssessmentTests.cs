using System;
using Guts.Common;
using Guts.Common.Extensions;
using Guts.Domain.ProjectTeamAssessmentAggregate;
using Guts.Domain.Tests.Builders;
using Guts.Domain.UserAggregate;
using Guts.Domain.ValueObjects;
using NUnit.Framework;

namespace Guts.Domain.Tests.ProjectTeamAssessmentAggregate
{
    public class PeerAssessmentTests
    {
        [Test]
        public void Constructor_ValidInput_ShouldInitializeCorrectly()
        {
            //Arrange
            int projectTeamAssessmentId = Random.Shared.NextPositive();
            User user = new UserBuilder().Build();
            User subject = new UserBuilder().Build();

            //Act
            var assessment = new PeerAssessment(projectTeamAssessmentId, user, subject);

            //Assert
            Assert.That(assessment.ProjectTeamAssessmentId, Is.EqualTo(projectTeamAssessmentId));
            Assert.That(assessment.User, Is.SameAs(user));
            Assert.That(assessment.Subject, Is.SameAs(subject));

            Assert.That(assessment.CooperationScore, Is.SameAs(AssessmentScore.NullScore));
            Assert.That(assessment.ContributionScore, Is.SameAs(AssessmentScore.NullScore));
            Assert.That(assessment.EffortScore, Is.SameAs(AssessmentScore.NullScore));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_ProjectTeamAssessmentIdNotPositive_ShouldThrowContractException(int invalidId)
        {
            User user = new UserBuilder().Build();
            User subject = new UserBuilder().Build();

            Assert.That(() => new PeerAssessment(invalidId, user, subject), Throws.InstanceOf<ContractException>());
        }

        [Test]
        public void Constructor_UserIsNull_ShouldThrowContractException()
        {
            int projectTeamAssessmentId = Random.Shared.NextPositive();
            User subject = new UserBuilder().Build();

            Assert.That(() => new PeerAssessment(projectTeamAssessmentId, null, subject), Throws.InstanceOf<ContractException>());
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_UserIdNotPositive_ShouldThrowContractException(int invalidUserId)
        {
            int projectTeamAssessmentId = Random.Shared.NextPositive();
            User user = new UserBuilder().WithId(invalidUserId).Build();
            User subject = new UserBuilder().Build();

            Assert.That(() => new PeerAssessment(projectTeamAssessmentId, user, subject), Throws.InstanceOf<ContractException>());
        }

        [Test]
        public void Constructor_SubjectIsNull_ShouldThrowContractException()
        {
            int projectTeamAssessmentId = Random.Shared.NextPositive();
            User user = new UserBuilder().Build();

            Assert.That(() => new PeerAssessment(projectTeamAssessmentId, user, null), Throws.InstanceOf<ContractException>());
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_SubjectIdNotPositive_ShouldThrowContractException(int invalidSubjectId)
        {
            int projectTeamAssessmentId = Random.Shared.NextPositive();
            User user = new UserBuilder().Build();
            User subject = new UserBuilder().WithId(invalidSubjectId).Build();

            Assert.That(() => new PeerAssessment(projectTeamAssessmentId, user, subject), Throws.InstanceOf<ContractException>());
        }

        [Test]
        public void SetScores_ShouldSetMatchingProperties()
        {
            //Arrange
            int projectTeamAssessmentId = Random.Shared.NextPositive();
            User user = new UserBuilder().Build();
            User subject = new UserBuilder().Build();
            var assessment = new PeerAssessment(projectTeamAssessmentId, user, subject);

            AssessmentScore cooperationScore = AssessmentScore.BelowAverage;
            AssessmentScore contributionScore = AssessmentScore.AboveAverage;
            AssessmentScore effortScore = AssessmentScore.Average;
            
            string explanation = Random.Shared.NextString();

            //Act
            assessment.SetScores(cooperationScore, contributionScore, effortScore, explanation);

            //Assert
            Assert.That(assessment.CooperationScore, Is.EqualTo(cooperationScore));
            Assert.That(assessment.ContributionScore, Is.EqualTo(contributionScore));
            Assert.That(assessment.EffortScore, Is.EqualTo(effortScore));
            Assert.That(assessment.Explanation, Is.EqualTo(explanation));
        }

        [Test]
        public void IsSelfAssessment_UserIsSameAsSubject_ShouldReturnTrue()
        {
            //Arrange
            int projectTeamAssessmentId = Random.Shared.NextPositive();
            User user = new UserBuilder().Build();
            User subject = user;
            var assessment = new PeerAssessment(projectTeamAssessmentId, user, subject);

            //Act
            bool result = assessment.IsSelfAssessment;

            //Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsSelfAssessment_UserDifferentFromSubject_ShouldReturnFalse()
        {
            //Arrange
            int projectTeamAssessmentId = Random.Shared.NextPositive();
            User user = new UserBuilder().Build();
            User subject = new UserBuilder().Build();
            var assessment = new PeerAssessment(projectTeamAssessmentId, user, subject);

            //Act
            bool result = assessment.IsSelfAssessment;

            //Assert
            Assert.That(result, Is.False);
        }
    }
}