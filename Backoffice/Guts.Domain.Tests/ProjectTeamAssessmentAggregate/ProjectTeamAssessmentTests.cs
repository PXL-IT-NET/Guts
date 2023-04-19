using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Guts.Common;
using Guts.Common.Extensions;
using Guts.Domain.ProjectTeamAggregate;
using Guts.Domain.ProjectTeamAssessmentAggregate;
using Guts.Domain.Tests.Builders;
using Guts.Domain.UserAggregate;
using Guts.Domain.ValueObjects;
using Moq;
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

        [Test]
        public void GetMissingPeerAssessmentsOf_UserNotATeamMember_ShouldThrowContractException()
        {
            //Arrange
            User nonTeamUser = new UserBuilder().Build();

            //Act + Assert
            Assert.That(
                () => _projectTeamAssessment.GetMissingPeerAssessmentsOf(nonTeamUser.Id),
                Throws.InstanceOf<ContractException>());
        }

        [Test]
        public void GetMissingPeerAssessmentsOf_NoPeerAssessmentsStoredYet_ShouldReturnANewPeerAssessmentForEachTeamMember()
        {
            //Arrange
            User user = _projectTeamAssessment.Team.TeamUsers.NextRandomItem().User; ;

            //Act
            IReadOnlyList<IPeerAssessment> results = _projectTeamAssessment.GetMissingPeerAssessmentsOf(user.Id);

            //Assert
            Assert.That(results, Has.Count.EqualTo(_projectTeamAssessment.Team.TeamUsers.Count));
            Assert.That(results, Has.All.Matches<IPeerAssessment>(pa => pa.ContributionScore == AssessmentScore.NoAddedValue));
            Assert.That(results, Has.All.Matches<IPeerAssessment>(pa => pa.EffortScore == AssessmentScore.NoAddedValue));
            Assert.That(results, Has.All.Matches<IPeerAssessment>(pa => pa.CooperationScore == AssessmentScore.NoAddedValue));
            Assert.That(results, Has.All.Matches<IPeerAssessment>(pa => pa.User.Id == user.Id));
            foreach (IProjectTeamUser teamUser in _projectTeamAssessment.Team.TeamUsers)
            {
                Assert.That(results, Has.One.Matches<IPeerAssessment>(pa => pa.Subject.Id == teamUser.UserId));
            }
        }

        [Test]
        public void GetMissingPeerAssessmentsOf_NoMissingPeerAssessments_ShouldReturnEmptyList()
        {
            //Arrange
            _projectTeamAssessment = new ProjectTeamAssessmentBuilder().WithAllPeerAssessmentsAdded().Build();
            User user = _projectTeamAssessment.Team.TeamUsers.NextRandomItem().User; ;

            //Act
            IReadOnlyList<IPeerAssessment> results = _projectTeamAssessment.GetMissingPeerAssessmentsOf(user.Id);

            //Assert
            Assert.That(results, Has.Count.Zero);
        }

        [Test]
        [TestCase(4, 3, 3)]
        [TestCase(3, 5, 3)]
        [TestCase(3, 3, 4)]
        [TestCase(1, 3, 3)]
        [TestCase(3, 2, 3)]
        [TestCase(3, 3, 2)]
        public void ValidateAssessmentsOf_AllScoresAboveOrBelowAverage_ShouldThrowContractException(int contributionScore, int effortScore, int cooperationScore)
        {
            //Arrange
            var builder = new ProjectTeamAssessmentBuilder().WithAllPeerAssessmentsAdded();
            _projectTeamAssessment = builder.Build();
            User user = _projectTeamAssessment.Team.TeamUsers.NextRandomItem().User;

            foreach (IProjectTeamUser teamUser in _projectTeamAssessment.Team.TeamUsers)
            {
                builder.WithPeerAssessment(user, teamUser.User, contributionScore, effortScore, cooperationScore);
            }

            //Act + Assert
            Assert.That(() => _projectTeamAssessment.ValidateAssessmentsOf(user.Id), Throws.InstanceOf<ContractException>());
        }

        [Test]
        public void ValidateAssessmentsOf_NotAllPeersAssessed_ShouldDoNothing()
        {
            //Arrange
            var builder = new ProjectTeamAssessmentBuilder().WithAllPeerAssessmentsAdded();
            _projectTeamAssessment = builder.Build();
            User user = _projectTeamAssessment.Team.TeamUsers.NextRandomItem().User;

            IEnumerable<IProjectTeamUser> subTeamUsers = _projectTeamAssessment.Team.TeamUsers.Take(2);

            foreach (IProjectTeamUser teamUser in subTeamUsers)
            {
                builder.WithPeerAssessment(user, teamUser.User, AssessmentScore.NoAddedValue, AssessmentScore.NoAddedValue, AssessmentScore.NoAddedValue);
            }

            //Act
            _projectTeamAssessment.ValidateAssessmentsOf(user.Id);
        }

        [Test]
        public void GetPeersThatNeedToEvaluateOthers_AllPeerAssessmentsFilledIn_ShouldReturnEmptyList()
        {
            //Arrange
            var builder = new ProjectTeamAssessmentBuilder().WithAllPeerAssessmentsAdded();
            _projectTeamAssessment = builder.Build();

            //Act
            IReadOnlyList<User> results = _projectTeamAssessment.GetPeersThatNeedToEvaluateOthers();

            //Assert
            Assert.That(results, Has.Count.Zero);
        }

        [Test]
        public void GetPeersThatNeedToEvaluateOthers_NoPeerAssessmentsFilledIn_ShouldReturnAllTeamUsers()
        {
            //Arrange
            var builder = new ProjectTeamAssessmentBuilder();
            _projectTeamAssessment = builder.Build();

            //Act
            IReadOnlyList<User> results = _projectTeamAssessment.GetPeersThatNeedToEvaluateOthers();

            //Assert
            Assert.That(results, Has.Count.EqualTo(_projectTeamAssessment.Team.TeamUsers.Count));
            Assert.That(results, Has.All.Matches<User>(peer => _projectTeamAssessment.Team.TeamUsers.Any(tu => tu.UserId == peer.Id)));
        }

        [Test]
        public void GetPeersThatNeedToEvaluateOthers_PeerAssessmentsOfOneUserMissing_ShouldReturnThatUser()
        {
            //Arrange
            var builder = new ProjectTeamAssessmentBuilder();
            _projectTeamAssessment = builder.Build();
            User user = _projectTeamAssessment.Team.TeamUsers.NextRandomItem().User;
            builder.WithAllAssessmentsAddedExceptAssessmentsOf(user.Id);

            //Act
            IReadOnlyList<User> results = _projectTeamAssessment.GetPeersThatNeedToEvaluateOthers();

            //Assert
            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results, Has.One.EqualTo(user));
        }

        [Test]
        public void GetPeerAssessmentsOf_ShouldReturnAllPeerAssessmentsOfUser()
        {
            //Arrange
            var builder = new ProjectTeamAssessmentBuilder().WithAllPeerAssessmentsAdded();
            _projectTeamAssessment = builder.Build();
            User user = _projectTeamAssessment.Team.TeamUsers.NextRandomItem().User;

            //Act
            IReadOnlyList<IPeerAssessment> results = _projectTeamAssessment.GetPeerAssessmentsOf(user.Id);

            //Assert
            Assert.That(results, Has.Count.EqualTo(_projectTeamAssessment.Team.TeamUsers.Count));
            Assert.That(results, Has.All.Matches<IPeerAssessment>(pa => pa.User.Id == user.Id));
        }

        [Test]
        public void GetPeerAssessmentsOf_UserNotInTeam_ShouldThrowContractException()
        {
            //Arrange
            var builder = new ProjectTeamAssessmentBuilder().WithAllPeerAssessmentsAdded();
            _projectTeamAssessment = builder.Build();
            User nonTeamUser = new UserBuilder().Build();

            //Act + Assert
            Assert.That(() => _projectTeamAssessment.GetPeerAssessmentsOf(nonTeamUser.Id), Throws.InstanceOf<ContractException>());
        }

        [Test]
        public void GetAssessmentResultFor_ShouldReturnResultWithUserAsSubject()
        {
            //Arrange
            var builder = new ProjectTeamAssessmentBuilder().WithAllPeerAssessmentsAdded();
            _projectTeamAssessment = builder.Build();
            User user = _projectTeamAssessment.Team.TeamUsers.NextRandomItem().User;

            IAssessmentResult expectedResult = new Mock<IAssessmentResult>().Object;
            builder.AssessmentResultFactoryMock
                .Setup(factory => factory.Create(It.IsAny<User>(), It.IsAny<IReadOnlyCollection<IPeerAssessment>>()))
                .Returns(expectedResult);

            //Act
            IAssessmentResult result = _projectTeamAssessment.GetAssessmentResultFor(user.Id, builder.AssessmentResultFactoryMock.Object);

            //Assert
            builder.AssessmentResultFactoryMock.Verify(factory => factory.Create(user, _projectTeamAssessment.PeerAssessments), Times.Once);
            Assert.That(result, Is.SameAs(expectedResult));
        }

        [Test]
        public void GetAssessmentResultFor_UserNotInTeam_ShouldThrowContractException()
        {
            //Arrange
            var builder = new ProjectTeamAssessmentBuilder().WithAllPeerAssessmentsAdded();
            _projectTeamAssessment = builder.Build();
            User nonTeamUser = new UserBuilder().Build();

            //Act + Assert
            Assert.That(() => _projectTeamAssessment.GetAssessmentResultFor(nonTeamUser.Id, builder.AssessmentResultFactoryMock.Object), Throws.InstanceOf<ContractException>());
        }
    }
}