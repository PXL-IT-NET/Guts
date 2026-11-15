using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain.DeletionJobAggregate;

namespace Guts.Application.Repositories;

public interface IDeletionJobRepository
{
    Task<DeletionJob> GetByIdAsync(Guid jobId);
    Task<IReadOnlyList<DeletionJob>> GetActiveAsync(DeletionTargetType? targetType = null);
    Task<DeletionJob> AddAsync(DeletionJob job);
    Task UpdateAsync(DeletionJob job);
}
