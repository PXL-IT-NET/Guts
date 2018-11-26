using System.Collections.Generic;

namespace Guts.Business
{
    public class AssignmentStatisticsDto
    {
        public int AssignmentId { get; set; }
        public IList<TestPassageStatistic> TestPassageStatistics { get; set; }
    }
}