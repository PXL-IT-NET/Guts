using System.Collections.Generic;

namespace Guts.Domain
{
    public class ProjectTeam : IDomainObject
    {
        public int Id { get; set; }

        public virtual Project Project { get; set; }
        public int ProjectId { get; set; }

        public virtual ICollection<ProjectTeamUser> TeamUsers { get; set; }
    }
}