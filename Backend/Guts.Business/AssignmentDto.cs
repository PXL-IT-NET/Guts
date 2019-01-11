using System;
using System.ComponentModel.DataAnnotations;

namespace Guts.Business
{
    public class AssignmentDto
    {
        [Required]
        [MaxLength(20)]
        public string CourseCode { get; set; }

        [Required]
        [MaxLength(20)]
        public string TopicCode { get; set; }

        [Required]
        [MaxLength(20)]
        public string AssignmentCode { get; set; }
    }
}