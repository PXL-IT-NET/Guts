using Guts.Business;
using Guts.Domain;

namespace Guts.Api.Models.Converters
{
    public interface IExerciseConverter
    {
        ExerciseDetailModel ToExerciseDetailModel(Exercise exercise, AssignmentResultDto results, ExerciseTestRunInfoDto testRunInfo);
    }
}