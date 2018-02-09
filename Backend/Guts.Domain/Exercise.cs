using System.Collections.Generic;

namespace Guts.Domain
{
    public class Exercise : IDomainObject
    {
        public int Id { get; set; }

        public virtual Chapter Chapter { get; set; }
        public int ChapterId { get; set; }

        public int Number { get; set; }

        public virtual ICollection<Test> Tests { get; set; }

        public virtual ICollection<TestRun> TestRuns { get; set; }
    }
}