using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Guts.Application.Converters;
using Guts.Application.Dtos;
using Guts.Application.Repositories;
using Guts.Domain.DeletionJobAggregate;
using Microsoft.Extensions.Logging;

namespace Guts.Application.Services.Deletion;

internal class DeletionJobService(
    IDeletionJobRepository deletionJobRepository,
    IChunkedDeletionRepository chunkedDeletionRepository,
    IDeletionJobFactory deletionJobFactory,
    IDeletionJobQueue deletionJobQueue,
    IDeletionJobConverter deletionJobConverter,
    ILogger<DeletionJobService> logger,
    TimeProvider timeProvider = null)
    : IDeletionJobService
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task<DeletionJobDto> GetJobStatusAsync(Guid jobId)
    {
        DeletionJob job = await deletionJobRepository.GetByIdAsync(jobId);
        return deletionJobConverter.ToDto(job);
    }

    public async Task<IReadOnlyList<DeletionJobDto>> GetActiveJobsAsync(DeletionTargetType? targetType = null)
    {
        IReadOnlyList<DeletionJob> activeJobs = await deletionJobRepository.GetActiveAsync(targetType);
        return activeJobs.Select(deletionJobConverter.ToDto).ToList();
    }

    public async Task<DeletionJobDto> QueueDeletionJobAsync(
        DeletionTargetType targetType,
        int targetId,
        string targetLabel,
        Func<DeletionJob, Task> persistAtomicAction)
    {
        ArgumentNullException.ThrowIfNull(persistAtomicAction);

        DateTime utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        DeletionJob job = deletionJobFactory.CreateNew(targetType, targetId, targetLabel, utcNow);
        await persistAtomicAction(job);
        await deletionJobQueue.EnqueueAsync(job.Id);
        return deletionJobConverter.ToDto(job);
    }

    public async Task<int> ProcessPendingJobsAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        int effectiveBatchSize = batchSize > 0 ? batchSize : IDeletionJobService.DefaultBatchSize;
        IReadOnlyList<DeletionJob> jobs = await deletionJobRepository.GetActiveAsync();
        int processedCount = 0;

        foreach (DeletionJob job in jobs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await ProcessSingleJobAsync(job, effectiveBatchSize, cancellationToken))
            {
                processedCount++;
            }
        }

        return processedCount;
    }

    private async Task<bool> ProcessSingleJobAsync(DeletionJob job, int batchSize, CancellationToken cancellationToken)
    {
        try
        {
            long remainingRows = await chunkedDeletionRepository.CountDependentAndRootRowsAsync(
                job.TargetType,
                job.TargetId,
                cancellationToken);
            long normalizedTotal = Math.Max(1, remainingRows);
            DateTime utcNow = _timeProvider.GetUtcNow().UtcDateTime;

            if (job.Status == DeletionJobStatus.Pending)
            {
                job.Start(normalizedTotal, utcNow, "Preparing cleanup");
            }
            else
            {
                job.ResetForResume(normalizedTotal, utcNow, "Resuming cleanup");
            }

            await deletionJobRepository.UpdateAsync(job);

            await chunkedDeletionRepository.ExecuteChunkedDeletionAsync(
                job.TargetType,
                job.TargetId,
                batchSize,
                async (rowsDeletedInBatch, phase) =>
                {
                    job.RecordBatchProgress(rowsDeletedInBatch, phase);
                    await deletionJobRepository.UpdateAsync(job);
                },
                cancellationToken);

            job.Complete(_timeProvider.GetUtcNow().UtcDateTime);
            await deletionJobRepository.UpdateAsync(job);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Chunked deletion failed for {TargetType} {TargetId} (Job {JobId}).",
                job.TargetType, job.TargetId, job.Id);

            if (job.Status is DeletionJobStatus.Pending or DeletionJobStatus.InProgress)
            {
                job.Fail(ex.Message, _timeProvider.GetUtcNow().UtcDateTime);
                await deletionJobRepository.UpdateAsync(job);
            }

            return false;
        }
    }
}
