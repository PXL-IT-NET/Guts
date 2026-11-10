namespace Guts.Domain.AssignmentAggregate;

public class RecommendedAssignment
{
    public int AssignmentId { get; }
    public string AssignmentCode { get; }
    public string AssignmentDescription { get; }
    public string TopicCode { get; }
    public string TopicDescription { get; }
    public int GreenTestsCount { get; }
    public int TotalTestsCount { get; }

    public RecommendedAssignment(
        int assignmentId,
        string assignmentCode,
        string assignmentDescription,
        string topicCode,
        string topicDescription,
        int greenTestsCount,
        int totalTestsCount)
    {
        AssignmentId = assignmentId;
        AssignmentCode = assignmentCode ?? string.Empty;
        AssignmentDescription = assignmentDescription ?? string.Empty;
        TopicCode = topicCode ?? string.Empty;
        TopicDescription = topicDescription ?? string.Empty;
        GreenTestsCount = greenTestsCount;
        TotalTestsCount = totalTestsCount;
    }
}

