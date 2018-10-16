using System;
using System.Collections.Generic;

namespace Guts.Api.Models
{
    public class ExerciseDetailModel
    {
        public int ExerciseId { get; set; }
        public string Code { get; set; }
        public int ChapterNumber { get; set; }
        public string CourseName { get; set; }
        public int CourseId { get; set; }
        public IList<TestResultModel> TestResults { get; set; }
        public DateTime? FirstRun { get; set; }
        public DateTime? LastRun { get; set; }
        public int NumberOfRuns { get; set; }
        public string SourceCode { get; set; }
    }
}