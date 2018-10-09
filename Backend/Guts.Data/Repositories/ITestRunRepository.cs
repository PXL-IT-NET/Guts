using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain;

namespace Guts.Data.Repositories
{
    public interface ITestRunRepository : IBasicRepository<TestRun>
    {
        Task<IList<TestRun>> GetUserTestRunsForExercise(int exerciseId, int userId, DateTime? date);
    }
}