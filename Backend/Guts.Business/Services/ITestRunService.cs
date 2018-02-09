using System.Threading.Tasks;
using Guts.Domain;

namespace Guts.Business.Services
{
    public interface ITestRunService
    {
        Task<TestRun> GetTestRunAsync(int id);
        Task<TestRun> RegisterRunAsync(TestRun run);
    }
}