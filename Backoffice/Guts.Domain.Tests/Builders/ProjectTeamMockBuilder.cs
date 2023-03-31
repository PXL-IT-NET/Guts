using System.Collections.Generic;
using Guts.Common.Extensions;
using Guts.Domain.ProjectTeamAggregate;
using Guts.Domain.UserAggregate;
using Moq;

namespace Guts.Domain.Tests.Builders
{
    internal class ProjectTeamMockBuilder : BaseBuilder<Mock<IProjectTeam>>
    {
        public ProjectTeamMockBuilder()
        {
            Item = new Mock<IProjectTeam>();
            Item.SetupGet(pt => pt.ProjectId).Returns(Random.NextPositive());
            Item.SetupGet(pa => pa.Name).Returns(Random.NextString());
            Item.SetupGet(pa => pa.TeamUsers).Returns(new List<IProjectTeamUser>());
        }

        public ProjectTeamMockBuilder WithMembers(int numberOfMembers)
        {
            var members = new List<IProjectTeamUser>();
            for (int i = 0; i < numberOfMembers; i++)
            {
                var memberMock = new Mock<IProjectTeamUser>();
                User user = new UserBuilder().Build();
                memberMock.SetupGet(m => m.UserId).Returns(user.Id);
                memberMock.SetupGet(m => m.User).Returns(user);

                members.Add(memberMock.Object);
            }
            Item.SetupGet(pa => pa.TeamUsers).Returns(members);
            return this;
        }
    }
}