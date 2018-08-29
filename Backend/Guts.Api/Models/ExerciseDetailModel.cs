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
        public int CourseId { get; set; }
        public IList<TestResultModel> TestResults { get; set; }
        public string FirstRun { get; set; }
        public string LastRun { get; set; }
        public int NumberOfRuns { get; set; }
    }
}