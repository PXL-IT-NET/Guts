using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Common;
using Guts.Domain.ProjectTeamAggregate;

namespace Guts.Domain.TopicAggregate.ProjectAggregate;

public class Project : Topic, IProject
{
    private HashSet<IProjectTeam> _teams = new HashSet<IProjectTeam>();

    public Project()
    {
    }

    public Project(int id) : base(id)
    {
    }

    public ICollection<IProjectAssessment> Assessments { get; set; } = new HashSet<IProjectAssessment>();

    public ICollection<IProjectTeam> Teams
    {
        get => _teams;
        set => _teams = value != null ? new HashSet<IProjectTeam>(value) : null;
    }

    public bool HasTeam(string teamName)
    {
        if (string.IsNullOrWhiteSpace(teamName) || _teams == null) return false;
        return _teams.Any(t => string.Equals(t.Name, teamName.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public IProjectTeam AddTeam(string teamName)
    {
        Contracts.Require(!string.IsNullOrWhiteSpace(teamName), "The team name is required.");
        var trimmedName = teamName.Trim();

        Contracts.Require(!HasTeam(trimmedName), "A team with the same name already exists");

        _teams ??= new HashSet<IProjectTeam>();
        var newTeam = new ProjectTeam
        {
            Name = trimmedName,
            ProjectId = Id,
            Project = this
        };
        _teams.Add(newTeam);
        return newTeam;
    }

    public void AddTeam(IProjectTeam team)
    {
        Contracts.Require(team != null, "The team cannot be null.");
        Contracts.Require(!string.IsNullOrWhiteSpace(team.Name), "The team name is required.");

        Contracts.Require(!HasTeam(team.Name), "A team with the same name already exists");

        _teams ??= new HashSet<IProjectTeam>();
        if (team is ProjectTeam concreteTeam)
        {
            concreteTeam.ProjectId = Id;
            concreteTeam.Project = this;
        }
        _teams.Add(team);
    }

    public IReadOnlyList<IProjectTeam> GenerateTeams(string teamBaseName, int teamNumberFrom, int teamNumberTo)
    {
        Contracts.Require(!string.IsNullOrWhiteSpace(teamBaseName), "The team base name is required.");
        Contracts.Require(teamNumberFrom > 0, "The start team number must be greater than zero.");
        Contracts.Require(teamNumberTo >= teamNumberFrom, "The end team number must be greater than or equal to the start team number.");
        Contracts.Require(teamNumberTo - teamNumberFrom <= 100, "Cannot generate more than 100 teams in a single request.");

        var generated = new List<IProjectTeam>();
        for (int teamNumber = teamNumberFrom; teamNumber <= teamNumberTo; teamNumber++)
        {
            string candidateName = $"{teamBaseName.Trim()} {teamNumber:D2}";
            if (!HasTeam(candidateName))
            {
                generated.Add(AddTeam(candidateName));
            }
        }
        return generated;
    }

    public IProjectTeam FindTeamOfUser(int userId)
    {
        return _teams?.FirstOrDefault(t => t.TeamUsers != null && t.TeamUsers.Any(tu => tu.UserId == userId));
    }

    public void ValidateUserCanJoinTeam(int teamId, int userId)
    {
        var currentTeam = FindTeamOfUser(userId);
        Contracts.Require(currentTeam is null, () => $"The user is already a member of '{currentTeam.Name}'. It is not allowed to be in multiple teams.");

        var targetTeam = _teams?.FirstOrDefault(t => t.Id == teamId);
        Contracts.Require(targetTeam != null, $"The team you want to join is not a team of project '{Code}'.");
    }

    public void EnsureTeamBelongsToProject(IProjectTeam team)
    {
        Contracts.Require(team != null && team.ProjectId == Id, "The team does not belong to the project");
    }

    public void EnsureUserIsMemberOfProject(int userId)
    {
        Contracts.Require(_teams != null && _teams.Count == 1, "The user is not a member ot the team");
    }

    public void LoadTeams(IEnumerable<IProjectTeam> teams)
    {
        Contracts.Require(teams != null, "Teams cannot be null.");
        _teams = new HashSet<IProjectTeam>(teams);
    }

    public void ClearTeams()
    {
        _teams = null;
    }
}