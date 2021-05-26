using System.Collections.Generic;
using Guts.Domain.ValueObjects;

namespace Guts.Business.Dtos
{
    public class SolutionDto
    {
        public int WriterId { get; set; }
        public string WriterName { get; set; }
        public IEnumerable<SolutionFile> SolutionFiles { get; set; }
    }
}