using Guts.Api.Models.AssignmentModels;

namespace Guts.Api.Models
{
    public class TopicAssignmentModel : AssignmentModel
    {
        public string TopicCode { get; set; }

        public string TopicDescription { get; set; }

        public int NumberOfTests { get; set; }
    }
}