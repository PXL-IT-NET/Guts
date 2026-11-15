using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Guts.Application.Dtos;
using Guts.Domain.DeletionJobAggregate;

namespace Guts.Application.Services.Deletion;

public interface IDeletionJobService
{
    public const int DefaultBatchSize = 1000;

    Task<DeletionJobDto> GetJobStatusAsync(Guid jobId);
    Task<IReadOnlyList<DeletionJobDto>> GetActiveJobsAsync(DeletionTargetType? targetType = null);
    Task<DeletionJobDto> QueueDeletionJobAsync(
        DeletionTargetType targetType,
        int targetId,
        string targetLabel,
        Func<DeletionJob, Task> persistAtomicAction);
    Task<int> ProcessPendingJobsAsync(int batchSize, CancellationToken cancellationToken = default);
}
