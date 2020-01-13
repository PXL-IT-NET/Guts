using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain.TestRunAggregate;

namespace Guts.Business.Repositories
{
    public interface ITestResultRepository : IBasicRepository<TestResult>
    {
        Task<IList<TestResult>> GetLastTestResultsOfAllUsers(int assignmentId, DateTime? dateUtc);
        Task<IList<TestResult>> GetLastTestResultsOfUser(int assignmentId, int userId, DateTime? dateUtc);
        Task<IList<TestResult>> GetLastTestResultsOfAllTeams(int assignmentId, DateTime? dateUtc);
        Task<IList<TestResult>> GetLastTestResultsOfTeam(int assignmentId, int teamId, DateTime? dateUtc);
        Task<IList<TestResult>> GetLastTestResultsOfAssignments(int[] assignmentIds, DateTime? dateUtc);
    }
}