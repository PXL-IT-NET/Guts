using System.Collections.Generic;
using Guts.Api.Models.AssignmentModels;

namespace Guts.Api.Models.ProjectModels
{
    public class ProjectDetailModel : TopicModel
    {
        public IList<AssignmentModel> Components { get; set; }
        public IList<TeamModel> Teams { get; set; }
    }
}