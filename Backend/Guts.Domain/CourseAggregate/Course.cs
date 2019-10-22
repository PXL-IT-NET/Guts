using System.ComponentModel.DataAnnotations;

namespace Guts.Domain.CourseAggregate
{
    public class Course : AggregateRoot
    {
        [Required]
        [MaxLength(20)]
        public string Code { get; set; }

        [Required]
        public string Name { get; set; }

        public Course()
        {

        }
    }
}