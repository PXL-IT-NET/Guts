using System.Collections.Generic;

namespace Guts.Domain
{
    public class Project : Assignment
    {
        public string Name { get; set; }

        public virtual Course Course { get; set; }
        public int CourseId { get; set; }

        public virtual Period Period { get; set; }
        public int PeriodId { get; set; }

        public virtual ICollection<ProjectTeam> Teams { get; set; }
    }
}