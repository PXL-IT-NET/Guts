namespace Guts.Api.Models.AssignmentModels
{
    public class AssignmentSummaryModel : AssignmentModel
    {
        public int NumberOfTests { get; set; }
        public int NumberOfPassedTests { get; set; }
        public int NumberOfFailedTests { get; set; }
    }
}