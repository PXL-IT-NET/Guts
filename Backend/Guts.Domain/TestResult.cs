using System;

namespace Guts.Domain
{
    public class TestResult : IDomainObject
    {
        public int Id { get; set; }

        public virtual TestRun TestRun { get; set; }
        public int TestRunId { get; set; }

        public virtual Test Test { get; set; }
        public int TestId { get; set; }

        public bool Passed { get; set; }

        public string Message { get; set; }

        public virtual User User { get; set; }
        public int UserId { get; set; }

        public DateTime CreateDateTime { get; set; }
    }
}