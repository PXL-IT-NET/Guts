using System.Threading.Tasks;
using Guts.Domain.TestRunAggregate;

namespace Guts.Business.Services
{
    public interface ITestRunService
    {
        Task<TestRun> GetTestRunAsync(int id);
        Task<TestRun> RegisterRunAsync(TestRun run);
    }
}