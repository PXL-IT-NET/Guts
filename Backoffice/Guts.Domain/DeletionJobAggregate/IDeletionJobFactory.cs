using System;

namespace Guts.Domain.DeletionJobAggregate;

public interface IDeletionJobFactory
{
    DeletionJob CreateNew(
        DeletionTargetType targetType,
        int targetId,
        string targetLabel,
        DateTime createdAtUtc);
}
