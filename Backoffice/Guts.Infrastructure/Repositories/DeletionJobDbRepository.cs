using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Application;
using Guts.Application.Repositories;
using Guts.Domain.DeletionJobAggregate;
using Microsoft.EntityFrameworkCore;

namespace Guts.Infrastructure.Repositories;

internal class DeletionJobDbRepository(GutsContext context) : IDeletionJobRepository
{
    public async Task<DeletionJob> GetByIdAsync(Guid jobId)
    {
        DeletionJob job = await context.DeletionJobs.FirstOrDefaultAsync(j => j.Id == jobId);
        if (job == null)
        {
            throw new DataNotFoundException();
        }

        return job;
    }

    public async Task<IReadOnlyList<DeletionJob>> GetActiveAsync(DeletionTargetType? targetType = null)
    {
        IQueryable<DeletionJob> query = context.DeletionJobs
            .Where(j => j.Status == DeletionJobStatus.Pending || j.Status == DeletionJobStatus.InProgress);

        if (targetType.HasValue)
        {
            query = query.Where(j => j.TargetType == targetType.Value);
        }

        return await query
            .OrderBy(j => j.CreatedAtUtc)
            .ToListAsync();
    }

    public async Task<DeletionJob> AddAsync(DeletionJob job)
    {
        await context.DeletionJobs.AddAsync(job);
        await context.SaveChangesAsync();
        return job;
    }

    public async Task UpdateAsync(DeletionJob job)
    {
        context.DeletionJobs.Update(job);
        await context.SaveChangesAsync();
    }
}
