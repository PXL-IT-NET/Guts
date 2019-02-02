using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain;

namespace Guts.Data.Repositories
{
    public interface ITestResultRepository : IBasicRepository<TestResult>
    {
        Task<IList<TestResult>> GetLastTestResults(int assignmentId, DateTime? dateUtc);
        Task<IList<TestResult>> GetLastTestResultsOfUser(int assignmentId, int userId, DateTime? dateUtc);
        Task<IList<TestResult>> GetLastTestResultsOfTeam(int assignmentId, int teamId, DateTime? dateUtc);
    }
}