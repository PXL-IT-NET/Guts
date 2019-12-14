using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.TestRunAggregate;

namespace Guts.Domain.TestAggregate
{
    public class Test : AggregateRoot
    {
        [Required]
        public string TestName { get; set; }

        public virtual Assignment Assignment { get; set; }
        public int AssignmentId { get; set; }

        public virtual ICollection<TestResult> Results { get; set; } = new HashSet<TestResult>();
    }
}