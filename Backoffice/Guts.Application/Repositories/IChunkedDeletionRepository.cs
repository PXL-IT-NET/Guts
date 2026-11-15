using System;
using System.Threading;
using System.Threading.Tasks;
using Guts.Domain.DeletionJobAggregate;

namespace Guts.Application.Repositories;

public interface IChunkedDeletionRepository
{
    Task<long> CountDependentAndRootRowsAsync(
        DeletionTargetType targetType,
        int targetId,
        CancellationToken cancellationToken = default);

    Task ExecuteChunkedDeletionAsync(
        DeletionTargetType targetType,
        int targetId,
        int batchSize,
        Func<int, string, Task> onBatchDeletedAsync,
        CancellationToken cancellationToken = default);
}
