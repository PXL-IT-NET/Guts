using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Common;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Guts.Domain.UserAggregate;

namespace Guts.Domain.ProjectTeamAggregate;

public class ProjectTeam : AggregateRoot, IProjectTeam
{
    private string _name;

    public string Name
    {
        get => _name;
        set
        {
            Contracts.Require(!string.IsNullOrWhiteSpace(value), "The team name is required.");
            _name = value;
        }
    }

    public IProject Project { get; set; }
    public int ProjectId { get; set; }

    public ICollection<IProjectTeamUser> TeamUsers { get; set; } = new HashSet<IProjectTeamUser>();
    public User GetTeamUser(int userId)
    {
        IProjectTeamUser teamUser = TeamUsers.SingleOrDefault(tu => tu.UserId == userId);
        Contracts.Require(teamUser != null, $"The user with id '{userId}' is not a member of the team.");
        return teamUser.User;
    }

    public bool HasMember(int userId)
    {
        return TeamUsers != null && TeamUsers.Any(tu => tu.UserId == userId);
    }

    public void AddUser(int userId)
    {
        Contracts.Require(userId > 0, "Invalid user id");
        Contracts.Require(!HasMember(userId), "The user is already a member of this team.");

        TeamUsers ??= new HashSet<IProjectTeamUser>();
        TeamUsers.Add(new ProjectTeamUser
        {
            ProjectTeamId = Id,
            UserId = userId
        });
    }

    public void AddUser(User user)
    {
        Contracts.Require(user != null, "User cannot be null.");
        AddUser(user.Id);
    }

    public void RemoveUser(int userId)
    {
        var member = TeamUsers?.FirstOrDefault(tu => tu.UserId == userId);
        Contracts.Require(member != null, "The user is not a member of the team.");
        TeamUsers.Remove(member);
    }

    public void Rename(string newName)
    {
        Contracts.Require(!string.IsNullOrWhiteSpace(newName), "The team name is required.");
        Name = newName.Trim();
    }

    public void EnsureBelongsTo(int courseId, string projectCode)
    {
        Contracts.Require(Project != null && Project.CourseId == courseId, $"Team with id '{Id}' is not linked to course with id '{courseId}'");
        Contracts.Require(Project != null && string.Equals((string)Project.Code, projectCode, StringComparison.OrdinalIgnoreCase), $"Team with id '{Id}' is not linked to project with code '{projectCode}'");
    }
}