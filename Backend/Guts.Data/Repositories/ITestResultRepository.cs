using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain;

namespace Guts.Data.Repositories
{
    public interface ITestResultRepository : IBasicRepository<TestResult>
    {
        Task<IList<TestResult>> GetLastTestResultsOfAssignmentAsync(int assignmentId, int? userId, DateTime? dateUtc);
    }
}