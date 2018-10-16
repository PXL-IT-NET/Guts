using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Guts.Domain
{
    public class Assignment : IDomainObject
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Code { get; set; }

        public string Description { get; set; }

        public virtual ICollection<Test> Tests { get; set; }

        public virtual ICollection<TestRun> TestRuns { get; set; }
    }
}