using System.Collections.Generic;

namespace Guts.Api.Models
{
    public class ProjectDetailModel : TopicModel
    {
        public IList<AssignmentModel> Components { get; set; }
        public IList<TeamModel> Teams { get; set; }
    }
}