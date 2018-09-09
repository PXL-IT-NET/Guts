using System;

namespace Guts.Business
{
    public class ExerciseTestRunInfoDto
    {
        public DateTime? FirstRunDateTime { get; set; }
        public DateTime? LastRunDateTime { get; set; }
        public int NumberOfRuns { get; set; }
        public string SourceCode { get; set; }
    }
}