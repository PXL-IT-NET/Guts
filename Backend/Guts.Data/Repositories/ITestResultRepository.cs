using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain;

namespace Guts.Data.Repositories
{
    public interface ITestResultRepository : IBasicRepository<TestResult>
    {
        Task<IList<TestWithLastUserResults>> GetLastTestResultsOfChapterAsync(int chapterId, int? userId, DateTime? dateUtc);
        Task<IList<TestWithLastUserResults>> GetLastTestResultsOfExerciseAsync(int exerciseId, int userId, DateTime? dateUtc);
    }
}