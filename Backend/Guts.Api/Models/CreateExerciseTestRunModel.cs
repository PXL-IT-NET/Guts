using System.ComponentModel.DataAnnotations;
using Guts.Business;

namespace Guts.Api.Models
{
    public class ExerciseCreateTestRunModel : CreateTestRunModelBase
    {
        [Required]
        public ExerciseDto Exercise { get; set; }
    }
}