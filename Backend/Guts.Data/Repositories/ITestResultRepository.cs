using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain;

namespace Guts.Data.Repositories
{
    public interface ITestResultRepository : IBasicRepository<TestResult>
    {
        Task<IList<AssignmentWithLastResultsOfMultipleUsers>> GetLastTestResultsOfChapterAsync(int chapterId, DateTime? dateUtc);
        Task<IList<AssignmentWithLastResultsOfUser>> GetLastTestResultsOfChapterAsync(int chapterId, int userId, DateTime? dateUtc);
        Task<AssignmentWithLastResultsOfUser> GetLastTestResultsOfExerciseAsync(int exerciseId, int userId, DateTime? dateUtc);
    }
}