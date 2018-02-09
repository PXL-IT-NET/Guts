using System.ComponentModel.DataAnnotations;

namespace Guts.Domain
{
    public class Course : IDomainObject
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Code { get; set; }

        [Required]
        public string Name { get; set; }
    }
}