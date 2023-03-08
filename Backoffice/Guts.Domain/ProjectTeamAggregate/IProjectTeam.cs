using System.Collections.Generic;
using Guts.Domain.TopicAggregate.ProjectAggregate;

namespace Guts.Domain.ProjectTeamAggregate
{
    public interface IProjectTeam : IEntity
    {
        string Name { get; }
        Project Project { get; }
        int ProjectId { get; }
        ICollection<ProjectTeamUser> TeamUsers { get; set; }
    }
}