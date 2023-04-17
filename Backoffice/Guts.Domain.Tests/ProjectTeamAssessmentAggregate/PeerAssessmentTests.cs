using System;
using System.Linq;
using Guts.Common.Extensions;
using Guts.Domain.ProjectTeamAssessmentAggregate;
using Guts.Domain.Tests.Builders;
using Guts.Domain.UserAggregate;
using Guts.Domain.ValueObjects;
using NUnit.Framework;

namespace Guts.Domain.Tests.ProjectTeamAssessmentAggregate
{
    public class PeerAssessmentTests : DomainTestBase
    {
        [Test]
        public void Constructor_ValidInput_ShouldInitializeCorrectly()
        {
            //Arrange
            int projectTeamAssessmentId = Random.NextPositive();
            User user = new UserBuilder().Build();
            User subject = new UserBuilder().Build();

            //Act
            var assessment = new PeerAssessment(projectTeamAssessmentId, user, subject);

            //Assert
            Assert.That(assessment.ProjectTeamAssessmentId, Is.EqualTo(projectTeamAssessmentId));
            Assert.That(assessment.User, Is.SameAs(user));
            Assert.That(assessment.Subject, Is.SameAs(subject));

            Assert.That(assessment.CooperationScore, Is.SameAs(AssessmentScore.NoAddedValue));
            Assert.That(assessment.ContributionScore, Is.SameAs(AssessmentScore.NoAddedValue));
            Assert.That(assessment.EffortScore, Is.SameAs(AssessmentScore.NoAddedValue));
        }

        [Test]
        public void SetScores_ShouldSetMatchingProperties()
        {
            //Arrange
            int projectTeamAssessmentId = Random.NextPositive();
            User user = new UserBuilder().Build();
            User subject = new UserBuilder().Build();
            var assessment = new PeerAssessment(projectTeamAssessmentId, user, subject);

            AssessmentScore cooperationScore = AssessmentScore.BelowAverage;
            AssessmentScore contributionScore = AssessmentScore.AboveAverage;
            AssessmentScore effortScore = AssessmentScore.Average;
            
            string explanation = Random.NextString();

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
            int projectTeamAssessmentId = Random.NextPositive();
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
            int projectTeamAssessmentId = Random.NextPositive();
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