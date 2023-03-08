using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Guts.Domain.TopicAggregate.ProjectAggregate;

namespace Guts.Domain.ProjectTeamAggregate
{
    public class ProjectTeam : AggregateRoot, IProjectTeam
    {
        [Required]
        public string Name { get; set; }

        public virtual Project Project { get; set; }
        public int ProjectId { get; set; }

        public virtual ICollection<ProjectTeamUser> TeamUsers { get; set; } = new HashSet<ProjectTeamUser>();
    }
}