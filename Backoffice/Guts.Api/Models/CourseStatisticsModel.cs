using System.Collections.Generic;

namespace Guts.Api.Models
{
    public class CourseStatisticsModel
    {
        public int CourseId { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public int TotalAssignments { get; set; }
        public int TotalTests { get; set; }
        public IList<CourseStudentStatisticsModel> Students { get; set; } = new List<CourseStudentStatisticsModel>();
    }

    public class CourseStudentStatisticsModel
    {
        public int StudentId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}".Trim();
        public int AssignmentsWorkedOn { get; set; }
        public int TotalPassedTests { get; set; }
        public int TotalTests { get; set; }
        public double CompletionPercentage { get; set; }
        public IList<CourseStudentChapterStatisticsModel> Chapters { get; set; } = new List<CourseStudentChapterStatisticsModel>();
    }

    public class CourseStudentChapterStatisticsModel
    {
        public int ChapterId { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public IList<CourseStudentAssignmentStatisticsModel> Assignments { get; set; } = new List<CourseStudentAssignmentStatisticsModel>();
    }

    public class CourseStudentAssignmentStatisticsModel
    {
        public int AssignmentId { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public int NumberOfPassedTests { get; set; }
        public int NumberOfTests { get; set; }
    }
}

