using System.Collections.Generic;

namespace Guts.Application.Dtos;

public class AssignmentStatisticsDto
{
    public int AssignmentId { get; set; }
    public IList<TestPassageStatisticDto> TestPassageStatistics { get; set; }
}