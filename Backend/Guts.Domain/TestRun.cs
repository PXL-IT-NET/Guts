using System;
using System.Collections.Generic;

namespace Guts.Domain
{
    public class TestRun : IDomainObject
    {
        public int Id { get; set; }

        public virtual Assignment Assignment { get; set; }
        public int AssignmentId { get; set; }

        public virtual User User { get; set; }
        public int UserId { get; set; }

        public DateTime CreateDateTime { get; set; }

        public virtual ICollection<TestResult> TestResults { get; set; }

        public string SourceCode { get; set; }
    }
}