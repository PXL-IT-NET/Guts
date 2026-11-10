using System;
using System.Linq;
using Guts.Common;
using Guts.Common.Extensions;
using Guts.Domain.ProjectTeamAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Guts.Domain.UserAggregate;
using NUnit.Framework;

namespace Guts.Domain.Tests.TopicAggregate;

[TestFixture]
public class ProjectTests
{
    [Test]
    public void AddTeam_WithValidName_ShouldCreateAndReturnNewTeam()
    {
        // Arrange
        var project = new Project(10) { Code = "Project_A" };

        // Act
        var team = project.AddTeam("Team Alpha");

        // Assert
        Assert.That(team, Is.Not.Null);
        Assert.That(team.Name, Is.EqualTo("Team Alpha"));
        Assert.That(team.ProjectId, Is.EqualTo(10));
        Assert.That(project.Teams, Has.Count.EqualTo(1));
        Assert.That(project.Teams.First(), Is.SameAs(team));
    }

    [Test]
    public void AddTeam_WithCaseInsensitiveDuplicateName_ShouldThrowContractException()
    {
        // Arrange
        var project = new Project(10);
        project.AddTeam("Team Alpha");

        // Act & Assert
        var ex = Assert.Throws<ContractException>(() => project.AddTeam("team alpha"));
        Assert.That(ex.Message, Does.Contain("A team with the same name already exists"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void AddTeam_WithNullOrWhitespaceName_ShouldThrowContractException(string invalidName)
    {
        var project = new Project(10);

        Assert.That(() => project.AddTeam(invalidName), Throws.InstanceOf<ContractException>());
    }

    [Test]
    public void GenerateTeams_WithValidRange_ShouldCreateNumberedTeams()
    {
        // Arrange
        var project = new Project(10);

        // Act
        var generated = project.GenerateTeams("Team", 1, 3);

        // Assert
        Assert.That(generated, Has.Count.EqualTo(3));
        Assert.That(generated.Select(t => t.Name), Is.EquivalentTo(new[] { "Team 01", "Team 02", "Team 03" }));
        Assert.That(project.Teams, Has.Count.EqualTo(3));
    }

    [Test]
    public void GenerateTeams_WhenSomeTeamsAlreadyExist_ShouldOnlyAddMissingTeams()
    {
        // Arrange
        var project = new Project(10);
        project.AddTeam("Team 02");

        // Act
        var generated = project.GenerateTeams("Team", 1, 3);

        // Assert
        Assert.That(generated, Has.Count.EqualTo(2));
        Assert.That(generated.Select(t => t.Name), Is.EquivalentTo(new[] { "Team 01", "Team 03" }));
        Assert.That(project.Teams, Has.Count.EqualTo(3));
    }

    [Test]
    public void GenerateTeams_WithInvalidRanges_ShouldThrowContractException()
    {
        var project = new Project(10);

        // teamNumberFrom <= 0
        Assert.Throws<ContractException>(() => project.GenerateTeams("Team", 0, 5));

        // teamNumberTo < teamNumberFrom
        Assert.Throws<ContractException>(() => project.GenerateTeams("Team", 5, 2));

        // range > 100
        Assert.Throws<ContractException>(() => project.GenerateTeams("Team", 1, 102));
    }

    [Test]
    public void FindTeamOfUser_WhenUserIsMember_ShouldReturnTeam()
    {
        // Arrange
        var project = new Project(10);
        project.AddTeam("Team 1");
        var team2 = project.AddTeam("Team 2");
        team2.TeamUsers.Add(new ProjectTeamUser { UserId = 42 });

        // Act
        var found = project.FindTeamOfUser(42);

        // Assert
        Assert.That(found, Is.Not.Null);
        Assert.That(found.Name, Is.EqualTo("Team 2"));
    }

    [Test]
    public void FindTeamOfUser_WhenUserIsNotMember_ShouldReturnNull()
    {
        var project = new Project(10);
        project.AddTeam("Team 1");

        Assert.That(project.FindTeamOfUser(99), Is.Null);
    }

    [Test]
    public void ValidateUserCanJoinTeam_WhenUserAlreadyInAnotherTeam_ShouldThrowContractException()
    {
        // Arrange
        var project = new Project(10) { Code = "PRJ" };
        var team1 = project.AddTeam("Team 1");
        var team2 = project.AddTeam("Team 2");
        team1.TeamUsers.Add(new ProjectTeamUser { UserId = 42 });

        // Act & Assert
        var ex = Assert.Throws<ContractException>(() => project.ValidateUserCanJoinTeam(team2.Id, 42));
        Assert.That(ex.Message, Does.Contain("The user is already a member of 'Team 1'. It is not allowed to be in multiple teams."));
    }

    [Test]
    public void ValidateUserCanJoinTeam_WhenTargetTeamDoesNotExistInProject_ShouldThrowContractException()
    {
        // Arrange
        var project = new Project(10) { Code = "PRJ" };
        project.AddTeam("Team 1");

        // Act & Assert
        var ex = Assert.Throws<ContractException>(() => project.ValidateUserCanJoinTeam(999, 42));
        Assert.That(ex.Message, Does.Contain("The team you want to join is not a team of project 'PRJ'."));
    }

    [Test]
    public void ValidateUserCanJoinTeam_WhenValid_ShouldNotThrow()
    {
        // Arrange
        var project = new Project(10) { Code = "PRJ" };
        var team = (ProjectTeam)project.AddTeam("Team 1");
        team.Id = 123;

        // Act & Assert
        Assert.DoesNotThrow(() => project.ValidateUserCanJoinTeam(123, 42));
    }

    [Test]
    public void EnsureTeamBelongsToProject_WhenProjectIdMatches_ShouldNotThrow()
    {
        var project = new Project(10);
        var team = new ProjectTeam { ProjectId = 10 };

        Assert.DoesNotThrow(() => project.EnsureTeamBelongsToProject(team));
    }

    [Test]
    public void EnsureTeamBelongsToProject_WhenProjectIdDiffers_ShouldThrowContractException()
    {
        var project = new Project(10);
        var team = new ProjectTeam { ProjectId = 99 };

        Assert.Throws<ContractException>(() => project.EnsureTeamBelongsToProject(team));
    }

    [Test]
    public void EnsureUserIsMemberOfProject_WhenTeamCountIsNotOne_ShouldThrowContractException()
    {
        var project = new Project(10);

        // 0 teams
        Assert.Throws<ContractException>(() => project.EnsureUserIsMemberOfProject(1));

        // 2 teams
        project.AddTeam("T1");
        project.AddTeam("T2");
        Assert.Throws<ContractException>(() => project.EnsureUserIsMemberOfProject(1));
    }
}
