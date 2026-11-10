using System;
using System.Linq;
using Guts.Common;
using Guts.Common.Extensions;
using Guts.Domain.ProjectTeamAggregate;
using Guts.Domain.Tests.Builders;
using Guts.Domain.UserAggregate;
using NUnit.Framework;

namespace Guts.Domain.Tests.ProjectTeamAggregate;

[TestFixture]
public class ProjectTeamTests
{
    [Test]
    public void NameSetter_ValidName_ShouldSetName()
    {
        string name = Random.Shared.NextString();
        var team = new ProjectTeam
        {
            Name = name
        };

        Assert.That(team.Name, Is.EqualTo(name));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void NameSetter_NullOrWhitespace_ShouldThrowContractException(string invalidName)
    {
        var team = new ProjectTeam();

        Assert.That(() => team.Name = invalidName, Throws.InstanceOf<ContractException>());
    }

    [Test]
    public void GetTeamUser_ExistingTeamMember_ShouldReturnMatchingUser()
    {
        User user1 = new UserBuilder().WithId().Build();
        User user2 = new UserBuilder().WithId().Build();
        var team = new ProjectTeam
        {
            Name = Random.Shared.NextString()
        };
        team.TeamUsers.Add(new ProjectTeamUser { UserId = user1.Id, User = user1, ProjectTeam = team });
        team.TeamUsers.Add(new ProjectTeamUser { UserId = user2.Id, User = user2, ProjectTeam = team });

        User result = team.GetTeamUser(user2.Id);

        Assert.That(result, Is.SameAs(user2));
    }

    [Test]
    public void GetTeamUser_UserNotInTeam_ShouldThrowContractException()
    {
        User user1 = new UserBuilder().WithId().Build();
        var team = new ProjectTeam
        {
            Name = Random.Shared.NextString()
        };
        team.TeamUsers.Add(new ProjectTeamUser { UserId = user1.Id, User = user1, ProjectTeam = team });
        int nonMemberUserId = user1.Id + 1;

        Assert.That(() => team.GetTeamUser(nonMemberUserId), Throws.InstanceOf<ContractException>());
    }

    [Test]
    public void HasMember_ShouldReturnTrueOnlyIfUserInTeam()
    {
        var team = new ProjectTeam();
        team.TeamUsers.Add(new ProjectTeamUser { UserId = 10 });

        Assert.That(team.HasMember(10), Is.True);
        Assert.That(team.HasMember(99), Is.False);
    }

    [Test]
    public void AddUser_WithValidId_ShouldAddTeamUser()
    {
        var team = new ProjectTeam { Id = 5 };

        team.AddUser(100);

        Assert.That(team.HasMember(100), Is.True);
        Assert.That(team.TeamUsers, Has.Count.EqualTo(1));
        var added = team.TeamUsers.First();
        Assert.That(added.UserId, Is.EqualTo(100));
        Assert.That(added.ProjectTeamId, Is.EqualTo(5));
    }

    [Test]
    public void AddUser_WhenAlreadyMember_ShouldThrowContractException()
    {
        var team = new ProjectTeam { Id = 5 };
        team.AddUser(100);

        Assert.Throws<ContractException>(() => team.AddUser(100));
    }

    [Test]
    public void RemoveUser_ExistingMember_ShouldRemoveFromTeamUsers()
    {
        var team = new ProjectTeam { Id = 5 };
        team.AddUser(100);
        Assert.That(team.HasMember(100), Is.True);

        team.RemoveUser(100);

        Assert.That(team.HasMember(100), Is.False);
        Assert.That(team.TeamUsers, Is.Empty);
    }

    [Test]
    public void RemoveUser_NonMember_ShouldThrowContractException()
    {
        var team = new ProjectTeam { Id = 5 };

        Assert.Throws<ContractException>(() => team.RemoveUser(99));
    }
}