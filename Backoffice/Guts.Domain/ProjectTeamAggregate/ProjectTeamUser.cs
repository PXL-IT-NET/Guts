using System.Collections.Generic;
using Guts.Domain.UserAggregate;

namespace Guts.Domain.ProjectTeamAggregate;

public class ProjectTeamUser : Entity, IProjectTeamUser
{
    public int Id { get; set; }
    public IProjectTeam ProjectTeam { get; set; }
    public int ProjectTeamId { get; set; }

    public User User { get; set; }
    public int UserId { get; set; }

    protected override IEnumerable<object> GetIdComponents()
    {
        yield return Id;
    }
}