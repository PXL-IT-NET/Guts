using System.Collections.Generic;

namespace Guts.Domain.CourseAggregate;

public class CourseProgressReport
{
    public int CourseId { get; }
    public string CourseCode { get; }
    public string CourseName { get; }
    public int TotalAssignments { get; }
    public int TotalTests { get; }
    public IReadOnlyList<CourseStudentProgress> Students { get; }

    public CourseProgressReport(
        int courseId,
        string courseCode,
        string courseName,
        int totalAssignments,
        int totalTests,
        IReadOnlyList<CourseStudentProgress> students)
    {
        CourseId = courseId;
        CourseCode = courseCode ?? string.Empty;
        CourseName = courseName ?? string.Empty;
        TotalAssignments = totalAssignments;
        TotalTests = totalTests;
        Students = students ?? [];
    }
}

