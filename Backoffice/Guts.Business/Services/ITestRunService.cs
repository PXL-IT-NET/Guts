using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain.TestRunAggregate;
using Guts.Domain.ValueObjects;

namespace Guts.Business.Services
{
    public interface ITestRunService
    {
        Task<TestRun> GetTestRunAsync(int id);
        Task<TestRun> RegisterRunAsync(TestRun run, IEnumerable<SolutionFile> solutionFiles);
    }
}