using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Guts.Domain.TestAggregate;
using Guts.Domain.TestRunAggregate;
using Guts.Domain.TopicAggregate;

namespace Guts.Domain.AssignmentAggregate
{
    public class Assignment : AggregateRoot
    {
        [Required]
        [MaxLength(20)]
        public string Code { get; set; }

        public string Description { get; set; }

        public virtual ICollection<Test> Tests { get; set; } = new HashSet<Test>();

        public virtual ICollection<TestRun> TestRuns { get; set; } = new HashSet<TestRun>();

        public virtual ICollection<TestCodeHash> TestCodeHashes { get; set; } = new HashSet<TestCodeHash>();

        public virtual Topic Topic { get; set; }
        public int TopicId { get; set; }

        public Assignment()
        {

        }
    }
}