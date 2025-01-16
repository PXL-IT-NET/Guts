using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Common;
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
            Item.SetupGet(pt => pt.ProjectId).Returns(Random.Shared.NextPositive());
            Item.SetupGet(pt => pt.Name).Returns(Random.Shared.NextString());
            Item.SetupGet(pt => pt.TeamUsers).Returns(new List<IProjectTeamUser>());
        }

        public ProjectTeamMockBuilder WithId()
        {
            Item.SetupGet(pt => pt.Id).Returns(Random.Shared.NextPositive());
            return this;
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
            Item.Setup(pt => pt.GetTeamUser(It.IsAny<int>())).Returns((int userId) =>
            {
                User member = members.SingleOrDefault(m => m.UserId == userId)?.User;
                Contracts.Require(member != null, "User is not a member" );
                return member;
            });
            return this;
        }
    }
}