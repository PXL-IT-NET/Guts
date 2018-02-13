namespace Guts.Api.Models
{
    public class ExerciseSummaryModel
    {
        public int ExerciseId { get; set; }
        public int Number { get; set; }
        public int NumberOfTests { get; set; }
        public int NumberOfPassedTests { get; set; }
        public int NumberOfFailedTests { get; set; }
    }
}