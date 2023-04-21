using System.Collections.Generic;
using Guts.Domain.AssessmentResultAggregate;
using Guts.Domain.ProjectTeamAssessmentAggregate;
using Guts.Domain.Tests.Builders;
using Guts.Domain.UserAggregate;
using Guts.Domain.ValueObjects;
using NUnit.Framework;

namespace Guts.Domain.Tests.AssessmentResultAggregate
{
    public class AssessmentSubResultFactoryTests : DomainTestBase
    {
        private AssessmentSubResult.Factory _factory;

        [SetUp]
        public void BeforeEachTest()
        {
            _factory = new AssessmentSubResult.Factory();
        }

        [Test]
        public void Create_ValidInput_ShouldCreateResultForSubject()
        {
            //Arrange
            User peer1 = new UserBuilder().WithId().Build();
            User peer2 = new UserBuilder().WithId().Build();

            var peerAssessments = new List<IPeerAssessment>
            {
                new PeerAssessmentBuilder()
                    .WithUserAndSubject(peer1, peer1)
                    .WithScores(AssessmentScore.Average, AssessmentScore.Average, AssessmentScore.Average)
                    .Build(),
                new PeerAssessmentBuilder()
                    .WithUserAndSubject(peer1, peer2)
                    .WithScores(AssessmentScore.Average, AssessmentScore.Average, AssessmentScore.Average)
                    .Build(),

                new PeerAssessmentBuilder()
                    .WithUserAndSubject(peer2, peer1)
                    .WithScores(AssessmentScore.WayBelowAverage, AssessmentScore.AboveAverage, AssessmentScore.WayAboveAverage)
                    .Build(),
                new PeerAssessmentBuilder()
                    .WithUserAndSubject(peer2, peer2)
                    .WithScores(AssessmentScore.AboveAverage, AssessmentScore.BelowAverage, AssessmentScore.WayAboveAverage)
                    .Build()
            };

            

            //Act
            IAssessmentSubResult result = _factory.Create(peer1.Id, peerAssessments, pa => pa.ContributionScore);

            //Assert
            Assert.That(result.AverageValue, Is.EqualTo( (3 + 3 + 1 + 4) / 4.0));
            Assert.That(result.AverageSelfValue, Is.EqualTo((3 + 4) / 2.0));
            Assert.That(result.AveragePeerValue, Is.EqualTo((3 + 1) / 2.0));

            Assert.That(result.Value, Is.EqualTo((3 + 1) / 2.0));
            Assert.That(result.SelfValue, Is.EqualTo(3.0));
            Assert.That(result.PeerValue, Is.EqualTo(1.0));

            Assert.That(result.Score, Is.EqualTo(AssessmentScore.BelowAverage));
            Assert.That(result.SelfScore, Is.EqualTo(AssessmentScore.Average));
            Assert.That(result.PeerScore, Is.EqualTo(AssessmentScore.WayBelowAverage));
        }
    }
}