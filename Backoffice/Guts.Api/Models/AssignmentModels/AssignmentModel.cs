using System.Collections.Generic;

namespace Guts.Api.Models.AssignmentModels
{
    public class AssignmentModel
    {
        public int AssignmentId { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public IList<TestModel> Tests { get; set; }
    }
}