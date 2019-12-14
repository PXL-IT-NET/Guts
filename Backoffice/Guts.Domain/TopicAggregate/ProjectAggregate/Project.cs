using System.Collections.Generic;
using Guts.Domain.ProjectTeamAggregate;

namespace Guts.Domain.TopicAggregate.ProjectAggregate
{
    public class Project : Topic
    {
        public Project()
        {
        }

        public Project(int id) : base(id)
        {
        }

        public virtual ICollection<ProjectTeam> Teams { get; set; } = new HashSet<ProjectTeam>();
    }
}