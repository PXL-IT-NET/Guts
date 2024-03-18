using System.Collections.Generic;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Guts.Domain.UserAggregate;

namespace Guts.Domain.ProjectTeamAggregate
{
    public interface IProjectTeam : IEntity
    {
        string Name { get; set; }
        IProject Project { get; }
        int ProjectId { get; }
        ICollection<IProjectTeamUser> TeamUsers { get; set; }

        User GetTeamUser(int userId);
    }
}