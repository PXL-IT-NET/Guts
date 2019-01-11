using System.Collections.Generic;

namespace Guts.Api.Models
{
    public class AssignmentStatisticsModel
    {
        public int AssignmentId { get; set; }
        public string Code { get; set; }
        public int TotalNumberOfUsers { get; set; }
        public IList<TestPassageStatisticModel> TestPassageStatistics { get; set; }
    }
}