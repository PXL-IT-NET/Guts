namespace Guts.Api.Models
{
    public class AssignmentSummaryModel
    {      
        public int AssignmentId { get; set; }
        public string Code { get; set; }
        public int NumberOfTests { get; set; }
        public int NumberOfPassedTests { get; set; }
        public int NumberOfFailedTests { get; set; }
    }
}