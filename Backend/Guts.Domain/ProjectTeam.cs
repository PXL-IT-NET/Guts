using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Guts.Domain
{
    public class ProjectTeam : IDomainObject
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public virtual Project Project { get; set; }
        public int ProjectId { get; set; }

        public virtual ICollection<ProjectTeamUser> TeamUsers { get; set; }
    }
}