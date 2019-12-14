using System.Collections.Generic;

namespace Guts.Api.Models
{
    public class AssignmentStatisticsModel: AssignmentModel
    {
        public int TotalNumberOfUnits { get; set; }
        public string Unit { get; set; }
        public IList<TestPassageStatisticModel> TestPassageStatistics { get; set; }
        
    }
}