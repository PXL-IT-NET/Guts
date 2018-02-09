using System.Collections.Generic;
using Guts.Business.Services;

namespace Guts.Business
{
    public class ExerciseResultDto
    {
        public int ExerciseId { get; set; }
        public IList<TestResultDto> TestResults { get; set; }
    }
}