using System.Collections.Generic;

namespace Guts.Domain
{
    public class Chapter : IDomainObject
    {
        public int Id { get; set; }
        public int Number { get; set; }

        public virtual Course Course { get; set; }
        public int CourseId { get; set; }

        public virtual Period Period { get; set; }
        public int PeriodId { get; set; }

        public virtual ICollection<Exercise> Exercises { get; set; }
    }
}