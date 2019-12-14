using System.Collections.Generic;

namespace Guts.Api.Models
{
    public class TopicSummaryModel
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public IList<AssignmentSummaryModel> AssignmentSummaries { get; set; }
    }
}
