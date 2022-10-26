using System;
using System.Collections.Generic;

namespace Guts.Api.Models
{
    public class AssignmentDetailModel
    {
        public int AssignmentId { get; set; }
        public string Code { get; set; }
        public string TopicCode { get; set; }
        public string CourseName { get; set; }
        public int CourseId { get; set; }
        public IList<TestResultModel> TestResults { get; set; }
        public DateTime? FirstRun { get; set; }
        public DateTime? LastRun { get; set; }
        public int NumberOfRuns { get; set; }
        public IList<SolutionFileOutputModel> SolutionFiles { get; set; }
    }
}