using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Guts.Business;

namespace Guts.Api.Models
{
    public class CreateTestRunModel
    {
        [Required]
        public ExerciseDto Exercise { get; set; }

        [Required]
        public IEnumerable<TestResultModel> Results { get; set; }
    }
}