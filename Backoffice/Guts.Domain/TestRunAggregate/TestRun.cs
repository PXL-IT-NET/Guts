using System;
using System.Collections.Generic;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.UserAggregate;

namespace Guts.Domain.TestRunAggregate
{
    public class TestRun : AggregateRoot
    {
        public virtual Assignment Assignment { get; set; }
        public int AssignmentId { get; set; }

        public virtual User User { get; set; }
        public int UserId { get; set; }

        public DateTime CreateDateTime { get; set; }

        public virtual ICollection<TestResult> TestResults { get; set; } = new HashSet<TestResult>();
    }
}