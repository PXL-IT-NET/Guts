using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Common.Extensions;
using Guts.Domain.ProjectTeamAggregate;
using Guts.Domain.ProjectTeamAssessmentAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Guts.Domain.UserAggregate;
using Guts.Domain.ValueObjects;
using Moq;

namespace Guts.Domain.Tests.Builders
{
    internal class ProjectTeamAssessmentBuilder : BaseBuilder<ProjectTeamAssessment>
    {
        public Mock<IAssessmentResultFactory> AssessmentResultFactoryMock { get; }

        public ProjectTeamAssessmentBuilder()
        {
            DateTime openOnUtc = DateTime.UtcNow.AddDays(-1);
            Mock<IProjectAssessment> projectAssessmentMock = new ProjectAssessmentMockBuilder().WithId().WithOpenOn(openOnUtc).Build();
            Mock<IProjectTeam> projectTeamMock = new ProjectTeamMockBuilder().WithMembers(4).Build();
            AssessmentResultFactoryMock = new Mock<IAssessmentResultFactory>();
            
            ConstructItem(projectAssessmentMock.Object, projectTeamMock.Object, AssessmentResultFactoryMock.Object);
        }

        public ProjectTeamAssessmentBuilder WithId()
        {
            SetProperty(pta => pta.Id, Random.NextPositive());
            return this;
        }

        public ProjectTeamAssessmentBuilder WithAllPeerAssessmentsAdded()
        {
            HashSet<IPeerAssessment> peerAssessments = GetFieldValue<HashSet<IPeerAssessment>>();
            peerAssessments.Clear();

            foreach (IProjectTeamUser teamUser in Item.Team.TeamUsers)
            {
                foreach (IProjectTeamUser subject in Item.Team.TeamUsers)
                {
                    IPeerAssessment peerAssessment= new PeerAssessmentBuilder()
                        .WithProjectTeamAssessmentId(Item.Id)
                        .WithUserAndSubject(teamUser.User, subject.User)
                        .Build();
                    peerAssessments.Add(peerAssessment);
                }
            }
            return this;
        }

        public ProjectTeamAssessmentBuilder WithPeerAssessment(User user, User subject, AssessmentScore contributionScore, AssessmentScore effortScore, AssessmentScore cooperationScore)
        {
            HashSet<IPeerAssessment> peerAssessments = GetFieldValue<HashSet<IPeerAssessment>>();
            IPeerAssessment peerAssessment =
                peerAssessments.FirstOrDefault(pa => pa.User.Id == user.Id && pa.Subject.Id == subject.Id);
            if (peerAssessment != null)
            {
                peerAssessments.Remove(peerAssessment);
            }
            peerAssessment = new PeerAssessmentBuilder()
                .WithProjectTeamAssessmentId(Item.Id)
                .WithUserAndSubject(user, subject)
                .WithScores(contributionScore, effortScore, cooperationScore)
                .Build();
            peerAssessments.Add(peerAssessment);

            return this;
        }

        public ProjectTeamAssessmentBuilder WithAllButOnePeerAssessmentsAdded()
        {
            HashSet<IPeerAssessment> peerAssessments = GetFieldValue<HashSet<IPeerAssessment>>();
            WithAllPeerAssessmentsAdded();
            peerAssessments.Remove(peerAssessments.NextRandomItem());
            return this;
        }

        public ProjectTeamAssessmentBuilder WithAllAssessmentsAddedExceptAssessmentsOf(int userId)
        {
            HashSet<IPeerAssessment> peerAssessments = GetFieldValue<HashSet<IPeerAssessment>>();
            WithAllPeerAssessmentsAdded();
            var peerAssessmentsToRemove = peerAssessments.Where(pa => pa.User.Id == userId).ToList();
            foreach (IPeerAssessment peerAssessment in peerAssessmentsToRemove)
            {
                peerAssessments.Remove(peerAssessment);
            }
            return this;
        }
    }
}