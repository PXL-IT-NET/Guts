namespace Guts.Api.Models
{
    public class ExerciseSummaryModel
    {      
        public int ExerciseId { get; set; }
        public string Code { get; set; }
        public int NumberOfTests { get; set; }
        public int NumberOfPassedTests { get; set; }
        public int NumberOfFailedTests { get; set; }
        public int NumberOfUsers { get; set; }
    }
}