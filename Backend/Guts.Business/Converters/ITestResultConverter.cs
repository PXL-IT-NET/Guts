using System.Collections.Generic;
using Guts.Data;

namespace Guts.Business.Converters
{
    public interface ITestResultConverter
    {
        IList<ExerciseResultDto> ToExerciseResultDto(IList<TestWithLastUserResults> testsWithLastUserResults);
    }
}
