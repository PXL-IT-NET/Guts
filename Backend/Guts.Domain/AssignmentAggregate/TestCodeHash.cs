using System.ComponentModel.DataAnnotations;

namespace Guts.Domain.AssignmentAggregate
{
    public class TestCodeHash : Entity
    {
        [Required]
        public string Hash { get; set; }

        public virtual Assignment Assignment { get; set; }
        public int AssignmentId { get; set; }
    }
}