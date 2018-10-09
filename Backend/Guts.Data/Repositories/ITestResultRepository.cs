using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain;

namespace Guts.Data.Repositories
{
    public interface ITestResultRepository : IBasicRepository<TestRun>
    {
        Task<IList<TestWithLastUserResults>> GetLastTestResultsOfChapterAsync(int chapterId, int? userId, DateTime? date);
        Task<IList<TestWithLastUserResults>> GetLastTestResultsOfExerciseAsync(int exerciseId, int userId, DateTime? date);
    }
}