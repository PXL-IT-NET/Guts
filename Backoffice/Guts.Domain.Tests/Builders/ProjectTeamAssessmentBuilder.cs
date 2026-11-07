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

namespace Guts.Domain.Tests.Builders;

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
        SetProperty(pta => pta.Id, Random.Shared.NextPositive());
        return this;
    }

    public ProjectTeamAssessmentBuilder WithMemberCount(int numberOfMembers)
    {
        Mock<IProjectTeam> projectTeamMock = new ProjectTeamMockBuilder().WithMembers(numberOfMembers).Build();
        ConstructItem(Item.ProjectAssessment, projectTeamMock.Object);
        return this;
    }

    public User AddTeamMember(DateTime joinedOnUtc)
    {
        var memberMock = new Mock<IProjectTeamUser>();
        User user = new UserBuilder().WithId().Build();
        memberMock.SetupGet(m => m.UserId).Returns(user.Id);
        memberMock.SetupGet(m => m.User).Returns(user);
        memberMock.SetupProperty(m => m.JoinedOnUtc, joinedOnUtc);

        Item.Team.TeamUsers.Add(memberMock.Object);
        return user;
    }

    public void RemoveTeamMember(int userId)
    {
        IProjectTeamUser existing = Item.Team.TeamUsers.FirstOrDefault(tu => tu.UserId == userId);
        if (existing != null)
        {
            Item.Team.TeamUsers.Remove(existing);
        }
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