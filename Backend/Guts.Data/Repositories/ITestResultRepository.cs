using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain;

namespace Guts.Data.Repositories
{
    public interface ITestResultRepository : IBasicRepository<TestResult>
    {
       // Task<IList<TestResult>> GetLastTestResultsOfChapterAsync(int chapterId, DateTime? dateUtc);
        Task<IList<TestResult>> GetLastTestResultsOfExerciseAsync(int exerciseId, int? userId, DateTime? dateUtc);
    }
}