using System.Collections.Generic;
using Guts.Api.Models.AssignmentModels;

namespace Guts.Api.Models
{
    public class TopicStatisticsModel
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public IList<AssignmentStatisticsModel> AssignmentStatistics { get; set; }
    }
}