using System.Collections.Generic;
using Guts.Domain.ValueObjects;

namespace Guts.Business.Dtos
{
    public class AssignmentSolutionDto
    {
        public int UserId { get; set; }
        public string UserFullName { get; set; }
        public IEnumerable<SolutionFile> SolutionFiles { get; set; }
    }
}