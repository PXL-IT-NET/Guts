using System;
using Guts.Application.Dtos;
using Guts.Domain.DeletionJobAggregate;

namespace Guts.Application.Converters;

public class DeletionJobConverter : IDeletionJobConverter
{
    public DeletionJobDto ToDto(DeletionJob job)
    {
        ArgumentNullException.ThrowIfNull(job);

        return new DeletionJobDto
        {
            JobId = job.Id,
            TargetType = job.TargetType,
            TargetId = job.TargetId,
            TargetLabel = job.TargetLabel,
            Status = job.Status,
            TotalRows = job.TotalRows,
            DeletedRows = job.DeletedRows,
            Percentage = job.CalculatePercentage(),
            CurrentPhase = job.CurrentPhase,
            ErrorMessage = job.ErrorMessage,
            CreatedAtUtc = job.CreatedAtUtc,
            StartedAtUtc = job.StartedAtUtc,
            CompletedAtUtc = job.CompletedAtUtc
        };
    }
}
