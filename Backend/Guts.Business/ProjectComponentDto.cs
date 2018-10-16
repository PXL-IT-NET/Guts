using System.ComponentModel.DataAnnotations;

namespace Guts.Business
{
    public class ProjectComponentDto
    {
        [Required]
        [MaxLength(20)]
        public string CourseCode { get; set; }

        [Required]
        [MaxLength(20)]
        public string ProjectCode { get; set; }

        [Required]
        [MaxLength(20)]
        public string ComponentCode { get; set; }
    }
}