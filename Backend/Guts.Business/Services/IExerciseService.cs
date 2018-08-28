using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain;

namespace Guts.Business.Services
{
    public interface IExerciseService
    {
        Task<Exercise> GetOrCreateExerciseAsync(ExerciseDto exerciseDto);
        Task LoadOrCreateTestsForExerciseAsync(Exercise exercise, IEnumerable<string> testNames);
        Task<ExerciseResultDto> GetResultsForUserAsync(int exerciseId, int userId);
        Task<ExerciseTestRunInfoDto> GetUserTestRunInfoForExercise(int exerciseId, int userId);
    }
}