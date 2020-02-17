using System;

namespace Guts.Business.Dtos
{
    public class AssignmentTestRunInfoDto
    {
        public DateTime? FirstRunDateTime { get; set; }
        public DateTime? LastRunDateTime { get; set; }
        public int NumberOfRuns { get; set; }
    }
}