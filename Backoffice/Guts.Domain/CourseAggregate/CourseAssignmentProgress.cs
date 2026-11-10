namespace Guts.Domain.CourseAggregate;

public class CourseAssignmentProgress
{
    public int AssignmentId { get; }
    public string Code { get; }
    public string Description { get; }
    public int NumberOfPassedTests { get; }
    public int NumberOfTests { get; }

    public CourseAssignmentProgress(
        int assignmentId,
        string code,
        string description,
        int numberOfPassedTests,
        int numberOfTests)
    {
        AssignmentId = assignmentId;
        Code = code ?? string.Empty;
        Description = description ?? string.Empty;
        NumberOfPassedTests = numberOfPassedTests;
        NumberOfTests = numberOfTests;
    }
}

