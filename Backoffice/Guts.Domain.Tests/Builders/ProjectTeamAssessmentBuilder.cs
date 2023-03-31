using System;
using System.Collections.Generic;
using Guts.Common.Extensions;
using Guts.Domain.ProjectTeamAggregate;
using Guts.Domain.ProjectTeamAssessmentAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Moq;

namespace Guts.Domain.Tests.Builders
{
    internal class ProjectTeamAssessmentBuilder : BaseBuilder<ProjectTeamAssessment>
    {
        public ProjectTeamAssessmentBuilder()
        {
            DateTime openOnUtc = DateTime.UtcNow.AddDays(-1);
            Mock<IProjectAssessment> projectAssessmentMock = new ProjectAssessmentMockBuilder().WithId().WithOpenOn(openOnUtc).Build();
            Mock<IProjectTeam> projectTeamMock = new ProjectTeamMockBuilder().WithMembers(4).Build();

            ConstructItem(projectAssessmentMock.Object, projectTeamMock.Object);
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

        public ProjectTeamAssessmentBuilder WithAllButOnePeerAssessmentsAdded()
        {
            HashSet<IPeerAssessment> peerAssessments = GetFieldValue<HashSet<IPeerAssessment>>();
            WithAllPeerAssessmentsAdded();
            peerAssessments.Remove(peerAssessments.NextRandomItem());
            return this;
        }
    }
}