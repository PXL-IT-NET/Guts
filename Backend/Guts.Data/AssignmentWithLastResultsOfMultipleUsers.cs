using System.Collections.Generic;
using Guts.Domain;

namespace Guts.Data
{
    public class AssignmentWithLastResultsOfMultipleUsers
    {
        public Assignment Assignment { get; set; }
        public IEnumerable<TestWithLastResultOfMultipleUsers> TestsWithLastResultOfMultipleUsers { get; set; }
    }
}