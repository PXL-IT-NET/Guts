using System.Collections.Generic;

namespace Guts.Domain
{
    public class Project : Topic
    {
        public virtual ICollection<ProjectTeam> Teams { get; set; }
    }
}