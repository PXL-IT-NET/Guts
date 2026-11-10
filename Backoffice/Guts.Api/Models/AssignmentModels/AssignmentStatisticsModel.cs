using System.Collections.Generic;

namespace Guts.Api.Models.AssignmentModels;

public class AssignmentStatisticsModel
{
    public int AssignmentId { get; set; }
    public string Code { get; set; }
    public string Description { get; set; }
    public int TotalNumberOfUnits { get; set; }
    public string Unit { get; set; }
    public IList<TestPassageStatisticModel> TestPassageStatistics { get; set; }
}