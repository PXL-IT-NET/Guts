using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Guts.Business.Dtos;

namespace Guts.Api.Models
{
    public class CreateAssignmentTestRunModel
    {
        [Required]
        public AssignmentDto Assignment { get; set; }

        [Required]
        public IEnumerable<TestResultModel> Results { get; set; }

        public IEnumerable<SolutionFileInputModel> SolutionFiles { get; set; }

        public string TestCodeHash { get; set; }
    }
}