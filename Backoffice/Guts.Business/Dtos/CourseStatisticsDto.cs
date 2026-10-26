using System.Collections.Generic;

namespace Guts.Business.Dtos;

public class CourseStatisticsDto
{
    public int CourseId { get; set; }
    public string CourseCode { get; set; }
    public string CourseName { get; set; }
    public int TotalAssignments { get; set; }
    public int TotalTests { get; set; }
    public IList<CourseStudentStatisticsDto> Students { get; set; } = new List<CourseStudentStatisticsDto>();
}

public class CourseStudentStatisticsDto
{
    public int StudentId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int AssignmentsWorkedOn { get; set; }
    public int TotalPassedTests { get; set; }
    public int TotalTests { get; set; }
    public double CompletionPercentage { get; set; }
    public IList<CourseStudentChapterStatisticsDto> Chapters { get; set; } = new List<CourseStudentChapterStatisticsDto>();
}

public class CourseStudentChapterStatisticsDto
{
    public int ChapterId { get; set; }
    public string Code { get; set; }
    public string Description { get; set; }
    public IList<CourseStudentAssignmentStatisticsDto> Assignments { get; set; } = new List<CourseStudentAssignmentStatisticsDto>();
}

public class CourseStudentAssignmentStatisticsDto
{
    public int AssignmentId { get; set; }
    public string Code { get; set; }
    public string Description { get; set; }
    public int NumberOfPassedTests { get; set; }
    public int NumberOfTests { get; set; }
}