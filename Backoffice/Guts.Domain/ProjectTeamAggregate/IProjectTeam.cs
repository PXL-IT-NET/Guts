using System.Collections.Generic;
using Guts.Domain.TopicAggregate.ProjectAggregate;

namespace Guts.Domain.ProjectTeamAggregate
{
    public interface IProjectTeam : IEntity
    {
        string Name { get; }
        IProject Project { get; }
        int ProjectId { get; }
        ICollection<IProjectTeamUser> TeamUsers { get; set; }
    }
}