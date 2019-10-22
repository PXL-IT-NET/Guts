using System.Collections.Generic;
using Guts.Domain.AssignmentAggregate;

namespace Guts.Data
{
    public class AssignmentWithLastResultsOfUser
    {
        public Assignment Assignment { get; set; }
        public IEnumerable<TestWithLastResultOfUser> TestsWithLastResultOfUser { get; set; }
    }
}