using System.Collections.Generic;

namespace Guts.Api.Models
{
    public class AssignmentStatisticsModel: AssignmentModel
    {
        public int TotalNumberOfUsers { get; set; }
        public IList<TestPassageStatisticModel> TestPassageStatistics { get; set; }
    }
}