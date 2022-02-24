using Guts.Domain.TopicAggregate.ProjectAggregate;
using System.Collections.Generic;

namespace Guts.Domain.ProjectTeamAggregate
{
    public interface IProjectTeam : IEntity
    {
        string Name { get; }
        Project Project { get;}
        int ProjectId { get; }
        ICollection<ProjectTeamUser> TeamUsers { get; set; }
    }
}