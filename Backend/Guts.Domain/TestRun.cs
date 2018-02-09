using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Guts.Domain
{
    public class TestRun : IDomainObject
    {
        public int Id { get; set; }

        public virtual Exercise Exercise { get; set; }
        public int ExerciseId { get; set; }

        public virtual User User { get; set; }
        public int UserId { get; set; }

        [Range(1, int.MaxValue)]
        public int Index { get; set; }

        public DateTime CreateDateTime { get; set; }

        public virtual ICollection<TestResult> TestResults { get; set; }
    }
}