using System.Collections.Generic;
using Guts.Business;
using Guts.Domain;

namespace Guts.Api.Models.Converters
{
    public interface IExerciseConverter
    {
        ExerciseDetailModel ToExerciseDetailModel(Exercise exercise, IList<TestResult> results, ExerciseTestRunInfoDto testRunInfo);
    }
}