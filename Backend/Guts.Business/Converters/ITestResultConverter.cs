using System;
using System.Collections.Generic;
using System.Text;
using Guts.Data;

namespace Guts.Business.Converters
{
    public interface ITestResultConverter
    {
        IList<ExerciseResultDto> ToExerciseResultDto(IList<TestWithLastUserResults> testsWithLastUserResults);
    }
}
