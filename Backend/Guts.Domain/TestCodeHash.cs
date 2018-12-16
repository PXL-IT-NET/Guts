using System.ComponentModel.DataAnnotations;

namespace Guts.Domain
{
    public class TestCodeHash
    {
        public int Id { get; set; }

        [Required]
        public string Hash { get; set; }

        public virtual Assignment Assignment { get; set; }
        public int AssignmentId { get; set; }
    }
}