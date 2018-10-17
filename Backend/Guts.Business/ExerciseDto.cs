using System;
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

        [Obsolete("This property should be removed when all clients send codes instead of numbers")]
        public int ExerciseNumber
        {
            set
            {
                if (value > 0)
                {
                    ExerciseCode = Convert.ToString(value);
                }
            }
        }

        public string ExerciseCode { get; set; }
    }
}