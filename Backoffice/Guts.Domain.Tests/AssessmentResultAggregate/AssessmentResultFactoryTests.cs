using System;
using System.Linq;
using Guts.Common;
using Guts.Common.Extensions;
using Guts.Domain.AssessmentResultAggregate;
using Guts.Domain.ProjectTeamAssessmentAggregate;
using Guts.Domain.Tests.Builders;
using Guts.Domain.UserAggregate;
using Guts.Domain.ValueObjects;
using Moq;
using NUnit.Framework;

namespace Guts.Domain.Tests.AssessmentResultAggregate
{
    public class AssessmentResultFactoryTests : DomainTestBase
    {
        private AssessmentResult.Factory _factory;
        private Mock<IAssessmentSubResultFactory> _assessmentSubResultFactoryMock;

        [SetUp]
        public void BeforeEachTest()
        {
            _assessmentSubResultFactoryMock = new Mock<IAssessmentSubResultFactory>();
            _factory = new AssessmentResult.Factory(_assessmentSubResultFactoryMock.Object);
        }

        [Test]
        public void Create_ValidInput_ShouldCreateResultForSubject()
        {
            //Arrange
            ProjectTeamAssessment projectTeamAssessment = new ProjectTeamAssessmentBuilder().WithAllPeerAssessmentsAdded().Build();
            User subject = projectTeamAssessment.Team.TeamUsers.NextRandomItem().User;

            var expectedPeerAssessments =
                projectTeamAssessment.PeerAssessments.Where(pa => pa.Subject.Id == subject.Id && pa.User.Id != subject.Id).ToList();
            var expectedSelfAssessment = projectTeamAssessment.PeerAssessments.Single(pa =>
                pa.Subject.Id == subject.Id && pa.User.Id == subject.Id);

            var dummyPeerAssessment = new PeerAssessmentBuilder().WithScores(AssessmentScore.WayBelowAverage, AssessmentScore.Average, AssessmentScore.AboveAverage).Build();

            var contributionResultMock = new Mock<IAssessmentSubResult>();
            _assessmentSubResultFactoryMock.Setup(
                    sf => sf.Create(subject.Id, projectTeamAssessment.PeerAssessments,
                        It.Is<Func<IPeerAssessment, double>>(calculateScoreFunction =>
                            calculateScoreFunction(dummyPeerAssessment) == dummyPeerAssessment.ContributionScore)))
                .Returns(contributionResultMock.Object).Verifiable();

            var effortResultMock = new Mock<IAssessmentSubResult>();
            _assessmentSubResultFactoryMock.Setup(
                    sf => sf.Create(subject.Id, projectTeamAssessment.PeerAssessments,
                        It.Is<Func<IPeerAssessment, double>>(calculateScoreFunction =>
                            calculateScoreFunction(dummyPeerAssessment) == dummyPeerAssessment.EffortScore)))
                .Returns(effortResultMock.Object).Verifiable();

            var cooperationResultMock = new Mock<IAssessmentSubResult>();
            _assessmentSubResultFactoryMock.Setup(
                    sf => sf.Create(subject.Id, projectTeamAssessment.PeerAssessments,
                        It.Is<Func<IPeerAssessment, double>>(calculateScoreFunction =>
                            calculateScoreFunction(dummyPeerAssessment) == dummyPeerAssessment.CooperationScore)))
                .Returns(cooperationResultMock.Object).Verifiable();

            var averageResultMock = new Mock<IAssessmentSubResult>();
            _assessmentSubResultFactoryMock.Setup(
                    sf => sf.Create(subject.Id, projectTeamAssessment.PeerAssessments,
                        It.Is<Func<IPeerAssessment, double>>(calculateScoreFunction =>
                            calculateScoreFunction(dummyPeerAssessment) == (dummyPeerAssessment.CooperationScore + dummyPeerAssessment.EffortScore + dummyPeerAssessment.ContributionScore) / 3.0)))
                .Returns(averageResultMock.Object).Verifiable();

            //Act
            IAssessmentResult result = _factory.Create(subject.Id, projectTeamAssessment, true);

            //Assert
            Assert.That(result.PeerAssessments, Is.EquivalentTo(expectedPeerAssessments));
            Assert.That(result.Subject, Is.SameAs(subject));
            Assert.That(result.SelfAssessment, Is.SameAs(expectedSelfAssessment));
            Assert.That(result.ContributionResult, Is.SameAs(contributionResultMock.Object));
            Assert.That(result.EffortResult, Is.SameAs(effortResultMock.Object));
            Assert.That(result.CooperationResult, Is.SameAs(cooperationResultMock.Object));
            Assert.That(result.AverageResult, Is.SameAs(averageResultMock.Object));

            _assessmentSubResultFactoryMock.Verify();
        }

        [Test]
        public void Create_SubjectNotInTeam_ShouldThrowContractException()
        {
            //Arrange
            ProjectTeamAssessment projectTeamAssessment = new ProjectTeamAssessmentBuilder().WithAllPeerAssessmentsAdded().Build();

            int unknownSubjectId = Random.NextPositive();
            while (projectTeamAssessment.Team.TeamUsers.Any(tu => tu.UserId == unknownSubjectId))
            {
                unknownSubjectId = Random.NextPositive();
            }

            //Act + Assert
            Assert.That(() => _factory.Create(unknownSubjectId, projectTeamAssessment, true),
                Throws.InstanceOf<ContractException>());

        }

        [Test]
        public void Create_ValidInput_NoPeerAssessmentsRequestedInResult_ShouldCreateResultForSubject()
        {
            //Arrange
            ProjectTeamAssessment projectTeamAssessment = new ProjectTeamAssessmentBuilder().WithAllPeerAssessmentsAdded().Build();
            User subject = projectTeamAssessment.Team.TeamUsers.NextRandomItem().User;

            var expectedSelfAssessment = projectTeamAssessment.PeerAssessments.Single(pa =>
                pa.Subject.Id == subject.Id && pa.User.Id == subject.Id);

            //Act
            IAssessmentResult result = _factory.Create(subject.Id, projectTeamAssessment, false);

            //Assert
            Assert.That(result.PeerAssessments, Has.Count.Zero);
            Assert.That(result.Subject, Is.SameAs(subject));
            Assert.That(result.SelfAssessment, Is.SameAs(expectedSelfAssessment));
        }
    }
}