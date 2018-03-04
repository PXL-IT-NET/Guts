using System.Collections.Generic;

namespace Guts.Business
{
    public class ExerciseResultDto
    {
        public int ExerciseId { get; set; }
        public IList<TestResultDto> TestResults { get; set; }
        public int UserCount { get; set; }
    }
}