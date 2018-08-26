using System.Collections.Generic;
using Guts.Business;

namespace Guts.Api.Models
{
    public class ExerciseDetailModel
    {
        public int ExerciseId { get; set; }
        public int Number { get; set; }
        public int ChapterNumber { get; set; }
        public string CourseName { get; set; }
        public IList<TestResultDto> TestResults { get; set; }
    }
}