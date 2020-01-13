namespace Guts.Domain.ExamAggregate
{
    public interface IAssignmentEvaluationScore
    {
        int AssignmentEvaluationId { get; }
        string AssignmentDescription { get; }
        double Score { get; }
        double MaximumScore { get; }
        int NumberOfTests { get; }
        int NumberOfPassedTests { get; set; }
    }
}