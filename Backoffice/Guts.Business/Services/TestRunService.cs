using System.Threading.Tasks;
using Guts.Business.Repositories;
using Guts.Domain.TestRunAggregate;

namespace Guts.Business.Services
{
    public class TestRunService : ITestRunService
    {
        private readonly ITestRunRepository _testRunRepository;

        public TestRunService(ITestRunRepository testRunRepository)
        {
            _testRunRepository = testRunRepository;
        }

        public async Task<TestRun> RegisterRunAsync(TestRun run)
        {
            return await _testRunRepository.AddAsync(run);
        }

        public async Task<TestRun> GetTestRunAsync(int id)
        {
            return await _testRunRepository.GetByIdAsync(id);
        }
    }
}
