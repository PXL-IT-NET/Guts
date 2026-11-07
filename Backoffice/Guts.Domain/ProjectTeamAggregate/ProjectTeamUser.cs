using System;
using Guts.Domain.UserAggregate;

namespace Guts.Domain.ProjectTeamAggregate;

public class ProjectTeamUser : Entity, IProjectTeamUser
{
    public IProjectTeam ProjectTeam { get; set; }
    public int ProjectTeamId { get; set; }

    public User User { get; set; }
    public int UserId { get; set; }

    public DateTime JoinedOnUtc { get; set; } = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
}