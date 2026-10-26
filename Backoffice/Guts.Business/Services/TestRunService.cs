using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Business.Repositories;
using Guts.Domain.TestRunAggregate;
using Guts.Domain.ValueObjects;

namespace Guts.Business.Services;

internal class TestRunService(
    ITestRunRepository testRunRepository,
    ISolutionFileRepository solutionFileRepository,
    IGamificationService gamificationService)
    : ITestRunService
{
    public async Task<TestRun> RegisterRunAsync(TestRun run, IEnumerable<SolutionFile> solutionFiles)
    {
        solutionFiles ??= new List<SolutionFile>();
        foreach (var solutionFile in solutionFiles)
        {
            SolutionFile previousFile = await solutionFileRepository.GetLatestForUserAsync(solutionFile.AssignmentId, solutionFile.UserId, solutionFile.FilePath);
            if (solutionFile.IsNewVersionOf(previousFile))
            {
                await solutionFileRepository.AddAsync(solutionFile);
            }
        }

        var savedRun = await testRunRepository.AddAsync(run);
        await gamificationService.ProcessTestRunAsync(savedRun);
        return savedRun;
    }

    public async Task<TestRun> GetTestRunAsync(int id)
    {
        return await testRunRepository.GetByIdAsync(id);
    }
}