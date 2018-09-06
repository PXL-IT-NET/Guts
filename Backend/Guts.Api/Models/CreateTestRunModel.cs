using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.AccessControl;
using Guts.Business;

namespace Guts.Api.Models
{
    public class CreateTestRunModel
    {
        [Required]
        public ExerciseDto Exercise { get; set; }

        [Required]
        public IEnumerable<TestResultModel> Results { get; set; }

        public string SourceCode { get; set; }
    }
}