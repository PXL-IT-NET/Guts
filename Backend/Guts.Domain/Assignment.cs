using System.Collections.Generic;

namespace Guts.Domain
{
    public class Assignment : IDomainObject
    {
        public int Id { get; set; }

        public virtual ICollection<Test> Tests { get; set; }

        public virtual ICollection<TestRun> TestRuns { get; set; }
    }
}