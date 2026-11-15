using System;
using Guts.Common.Extensions;
using Guts.Domain.DeletionJobAggregate;

namespace Guts.Domain.Tests.Builders;

internal class DeletionJobBuilder : BaseBuilder<DeletionJob>
{
    public DeletionJobBuilder()
    {
        Item = new DeletionJobFactory().CreateNew(
            DeletionTargetType.Course,
            Random.Shared.NextPositive(),
            "CS101 - Intro",
            DateTime.UtcNow);
    }

    public DeletionJobBuilder WithId(Guid id)
    {
        SetProperty(j => j.Id, id);
        return this;
    }

    public DeletionJobBuilder WithTarget(DeletionTargetType targetType, int targetId, string targetLabel)
    {
        SetProperty(j => j.TargetType, targetType);
        SetProperty(j => j.TargetId, targetId);
        SetProperty(j => j.TargetLabel, targetLabel);
        return this;
    }

    public DeletionJobBuilder WithStatus(DeletionJobStatus status)
    {
        SetProperty(j => j.Status, status);
        return this;
    }

    public DeletionJobBuilder WithProgress(long deletedRows, long totalRows, string currentPhase = "Deleting test runs")
    {
        SetProperty(j => j.DeletedRows, deletedRows);
        SetProperty(j => j.TotalRows, totalRows);
        SetProperty(j => j.CurrentPhase, currentPhase);
        return this;
    }
}
