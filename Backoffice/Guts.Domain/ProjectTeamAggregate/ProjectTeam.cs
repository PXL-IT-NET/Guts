using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Guts.Domain.TopicAggregate.ProjectAggregate;

namespace Guts.Domain.ProjectTeamAggregate
{
    public class ProjectTeam : AggregateRoot, IProjectTeam
    {
        [Required]
        public string Name { get; set; }

        public IProject Project { get; set; }
        public int ProjectId { get; set; }

        public ICollection<IProjectTeamUser> TeamUsers { get; set; } = new HashSet<IProjectTeamUser>();
    }
}