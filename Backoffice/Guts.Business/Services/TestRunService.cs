using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Business.Repositories;
using Guts.Domain.TestRunAggregate;
using Guts.Domain.ValueObjects;

namespace Guts.Business.Services
{
    internal class TestRunService : ITestRunService
    {
        private readonly ITestRunRepository _testRunRepository;
        private readonly ISolutionFileRepository _solutionFileRepository;

        public TestRunService(ITestRunRepository testRunRepository, ISolutionFileRepository solutionFileRepository)
        {
            _testRunRepository = testRunRepository;
            _solutionFileRepository = solutionFileRepository;
        }

        public async Task<TestRun> RegisterRunAsync(TestRun run, IEnumerable<SolutionFile> solutionFiles)
        {
            solutionFiles ??= new List<SolutionFile>();
            foreach (var solutionFile in solutionFiles)
            {
                SolutionFile previousFile  = await _solutionFileRepository.GetLatestForUserAsync(solutionFile.AssignmentId, solutionFile.UserId, solutionFile.FilePath);
                if (solutionFile.IsNewVersionOf(previousFile))
                {
                    await _solutionFileRepository.AddAsync(solutionFile);
                }
            }

            return await _testRunRepository.AddAsync(run);
        }

        public async Task<TestRun> GetTestRunAsync(int id)
        {
            return await _testRunRepository.GetByIdAsync(id);
        }
    }
}
