using System.Collections.Generic;

namespace Guts.Domain.CourseAggregate;

public class CourseChapterProgress
{
    public int ChapterId { get; }
    public string Code { get; }
    public string Description { get; }
    public IReadOnlyList<CourseAssignmentProgress> Assignments { get; }

    public CourseChapterProgress(
        int chapterId,
        string code,
        string description,
        IReadOnlyList<CourseAssignmentProgress> assignments)
    {
        ChapterId = chapterId;
        Code = code ?? string.Empty;
        Description = description ?? string.Empty;
        Assignments = assignments ?? [];
    }
}

