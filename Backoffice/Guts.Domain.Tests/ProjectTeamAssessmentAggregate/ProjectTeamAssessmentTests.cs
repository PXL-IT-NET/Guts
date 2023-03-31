using System.Linq;
using Guts.Common;
using Guts.Common.Extensions;
using Guts.Domain.ProjectTeamAssessmentAggregate;
using Guts.Domain.Tests.Builders;
using Guts.Domain.UserAggregate;
using Guts.Domain.ValueObjects;
using NUnit.Framework;

namespace Guts.Domain.Tests.ProjectTeamAssessmentAggregate
{
    public class ProjectTeamAssessmentTests : DomainTestBase
    {
        private IProjectTeamAssessment _projectTeamAssessment;

        [SetUp]
        public void BeforeEachTest()
        {
            _projectTeamAssessment = new ProjectTeamAssessmentBuilder().WithId().Build();
        }

        [Test]
        public void IsComplete_EachMemberHasEvaluatedSelfAndOthers_ShouldReturnTrue()
        {
            //Arrange
            _projectTeamAssessment = new ProjectTeamAssessmentBuilder().WithAllPeerAssessmentsAdded().Build();

            //Act + Assert
            Assert.That(_projectTeamAssessment.IsComplete, Is.True);
        }

        [Test]
        public void IsComplete_PeerAssessmentMissing_ShouldReturnFalse()
        {
            //Arrange
            _projectTeamAssessment = new ProjectTeamAssessmentBuilder().WithAllButOnePeerAssessmentsAdded().Build();

            //Act + Assert
            Assert.That(_projectTeamAssessment.IsComplete, Is.False);
        }

        [Test]
        public void AddOrUpdatePeerAssessment_ValidNonExistingPeerAssessment_ShouldAddTheAssessment()
        {
            //Arrange
            User user = _projectTeamAssessment.Team.TeamUsers.First().User;
            User subject = _projectTeamAssessment.Team.TeamUsers.Last().User;

            AssessmentScore cooperationScore = AssessmentScore.BelowAverage;
            AssessmentScore contributionScore = AssessmentScore.Average;
            AssessmentScore effortScore = AssessmentScore.AboveAverage;
            string explanation = Random.NextString();

            //Act
            IPeerAssessment result = _projectTeamAssessment.AddOrUpdatePeerAssessment(user.Id, subject.Id, cooperationScore, contributionScore, effortScore, explanation);

            //Assert
            Assert.That(result.User, Is.SameAs(user));
            Assert.That(result.Subject, Is.SameAs(subject));
            Assert.That(result.ProjectTeamAssessmentId, Is.EqualTo(_projectTeamAssessment.Id));

            Assert.That(result.ContributionScore, Is.EqualTo(contributionScore));
            Assert.That(result.EffortScore, Is.EqualTo(effortScore));
            Assert.That(result.CooperationScore, Is.EqualTo(cooperationScore));
            Assert.That(result.Explanation, Is.EqualTo(explanation));

            Assert.That(_projectTeamAssessment.PeerAssessments, Has.One.SameAs(result));
        }

        [Test]
        public void AddOrUpdatePeerAssessment_ValidExistingPeerAssessment_ShouldUpdateTheAssessment()
        {
            //Arrange
            _projectTeamAssessment = new ProjectTeamAssessmentBuilder().WithId().WithAllButOnePeerAssessmentsAdded().Build();

            IPeerAssessment peerAssessmentToUpdate = _projectTeamAssessment.PeerAssessments.NextRandomItem();

            AssessmentScore cooperationScore = AssessmentScore.BelowAverage;
            AssessmentScore contributionScore = AssessmentScore.Average;
            AssessmentScore effortScore = AssessmentScore.AboveAverage;
            string explanation = Random.NextString();

            //Act
            IPeerAssessment result = _projectTeamAssessment.AddOrUpdatePeerAssessment(peerAssessmentToUpdate.User.Id,
                peerAssessmentToUpdate.Subject.Id, cooperationScore, contributionScore, effortScore, explanation);

            //Assert
            Assert.That(peerAssessmentToUpdate, Is.SameAs(result));

            Assert.That(result.ContributionScore, Is.EqualTo(contributionScore));
            Assert.That(result.EffortScore, Is.EqualTo(effortScore));
            Assert.That(result.CooperationScore, Is.EqualTo(cooperationScore));
            Assert.That(result.Explanation, Is.EqualTo(explanation));
        }

        [Test]
        public void AddOrUpdatePeerAssessment_TeamAssessmentAlreadyCompleted_ShouldThrowContractException()
        {
            //Arrange
            _projectTeamAssessment = new ProjectTeamAssessmentBuilder().WithId().WithAllPeerAssessmentsAdded().Build();

            IPeerAssessment peerAssessmentToUpdate = _projectTeamAssessment.PeerAssessments.NextRandomItem();

            AssessmentScore cooperationScore = AssessmentScore.BelowAverage;
            AssessmentScore contributionScore = AssessmentScore.Average;
            AssessmentScore effortScore = AssessmentScore.AboveAverage;
            string explanation = Random.NextString();

            //Act + Assert
            Assert.That(
                () => _projectTeamAssessment.AddOrUpdatePeerAssessment(peerAssessmentToUpdate.User.Id,
                    peerAssessmentToUpdate.Subject.Id, 
                    cooperationScore, contributionScore, effortScore, explanation),
                Throws.InstanceOf<ContractException>());
        }

        [Test]
        public void AddOrUpdatePeerAssessment_UserNotPartOfTheTeam_ShouldThrowContractException()
        {
            //Arrange
            User invalidUser = new UserBuilder().WithId().Build();
            User subject = _projectTeamAssessment.Team.TeamUsers.Last().User;

            AssessmentScore cooperationScore = AssessmentScore.BelowAverage;
            AssessmentScore contributionScore = AssessmentScore.Average;
            AssessmentScore effortScore = AssessmentScore.AboveAverage;
            string explanation = Random.NextString();

            //Act + Assert
            Assert.That(
                () => _projectTeamAssessment.AddOrUpdatePeerAssessment(invalidUser.Id, subject.Id,
                    cooperationScore, contributionScore, effortScore, explanation),
                Throws.InstanceOf<ContractException>());
        }

        [Test]
        public void AddOrUpdatePeerAssessment_SubjectNotPartOfTheTeam_ShouldThrowContractException()
        {
            //Arrange
            User user = _projectTeamAssessment.Team.TeamUsers.Last().User;
            User invalidSubject = new UserBuilder().WithId().Build();

            AssessmentScore cooperationScore = AssessmentScore.BelowAverage;
            AssessmentScore contributionScore = AssessmentScore.Average;
            AssessmentScore effortScore = AssessmentScore.AboveAverage;
            string explanation = Random.NextString();

            //Act + Assert
            Assert.That(
                () => _projectTeamAssessment.AddOrUpdatePeerAssessment(user.Id, invalidSubject.Id,
                    cooperationScore, contributionScore, effortScore, explanation),
                Throws.InstanceOf<ContractException>());
        }

        //TODO: write missing tests
    }
}