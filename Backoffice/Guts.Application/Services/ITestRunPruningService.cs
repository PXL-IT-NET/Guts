using System.Threading;
using System.Threading.Tasks;
using Guts.Application.Dtos;

namespace Guts.Application.Services;

public interface ITestRunPruningService
{
    public const int DefaultBatchSize = 500;

    Task<TestRunPruningResult> PruneRedundantTestRunsAsync(
        int batchSize = DefaultBatchSize,
        CancellationToken cancellationToken = default);
}
