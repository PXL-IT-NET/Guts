﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain;

namespace Guts.Business.Services
{
    public interface IExerciseService
    {
        Task<Exercise> GetOrCreateExerciseAsync(ExerciseDto exerciseDto);
        Task LoadOrCreateTestsForExerciseAsync(Exercise exercise, IEnumerable<string> testNames);
    }
}