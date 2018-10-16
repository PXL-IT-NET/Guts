using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Guts.Domain
{
    public class Project : IDomainObject
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Code { get; set; }

        [Required]
        public string Description { get; set; }

        public virtual Course Course { get; set; }
        public int CourseId { get; set; }

        public virtual Period Period { get; set; }
        public int PeriodId { get; set; }

        public virtual ICollection<ProjectTeam> Teams { get; set; }

        public virtual ICollection<ProjectComponent> Components { get; set; }
    }
}