using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Guts.Domain
{
    public class Test : IDomainObject
    {
        public int Id { get; set; }

        [Required]
        public string TestName { get; set; }

        public virtual Exercise Exercise { get; set; }
        public int ExerciseId { get; set; }

        public virtual ICollection<TestResult> Results { get; set; }
    }
}