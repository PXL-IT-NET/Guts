using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Guts.Domain
{
    public class Topic : IDomainObject
    {
        private string _code;

        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Code
        {
            get => _code;
            set
            {
                _code = value;
                if (string.IsNullOrEmpty(Description))
                {
                    Description = value;
                }
            }
        }

        [Required]
        public string Description { get; set; }

        public virtual Course Course { get; set; }
        public int CourseId { get; set; }

        public virtual Period Period { get; set; }
        public int PeriodId { get; set; }

        public virtual ICollection<Assignment> Assignments { get; set; }
    }
}