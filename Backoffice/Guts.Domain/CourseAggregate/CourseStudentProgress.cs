using System.Collections.Generic;

namespace Guts.Domain.CourseAggregate;

public class CourseStudentProgress
{
    public int StudentId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public int TotalTests { get; init; }
    public int TotalPassedTests { get; init; }
    public int AssignmentsWorkedOn { get; init; }
    public double CompletionPercentage { get; init; }
    public IReadOnlyList<CourseChapterProgress> Chapters { get; init; } = [];
}
