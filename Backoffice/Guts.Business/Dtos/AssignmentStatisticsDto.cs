using System.Collections.Generic;

namespace Guts.Business.Dtos
{
    public class AssignmentStatisticsDto
    {
        public int AssignmentId { get; set; }
        public IList<TestPassageStatisticDto> TestPassageStatistics { get; set; }
    }
}