using System.ComponentModel.DataAnnotations;

namespace Guts.Business
{
    public class ExerciseDto
    {
        [Required]
        [MaxLength(20)]
        public string CourseCode { get; set; }

        [Range(1, int.MaxValue)]
        public int ChapterNumber { get; set; }

        [Range(1, int.MaxValue)]
        public int ExerciseNumber { get; set; }
    }
}