using System;

namespace Guts.Domain.DeletionJobAggregate;

public class DeletionJobFactory : IDeletionJobFactory
{
    public DeletionJob CreateNew(
        DeletionTargetType targetType,
        int targetId,
        string targetLabel,
        DateTime createdAtUtc)
    {
        return new DeletionJob(Guid.NewGuid(), targetType, targetId, targetLabel, createdAtUtc);
    }
}
