using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Application.Repositories;
using Guts.Domain.MessageTrackerAggregate;
using Guts.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Guts.Infrastructure.Repositories;

internal class MessageTrackerDbRepository : BaseDbRepository<IMessageTracker, MessageTracker>, IMessageTrackerRepository
{
    public MessageTrackerDbRepository(GutsContext context) : base(context)
    {
    }

    public async Task<IMessageTracker> FindByEntityAndEmailAsync(string entityType, int entityId, Email email)
    {
        return await _context.MessageTrackers
            .FirstOrDefaultAsync(mt => mt.EntityType == entityType && mt.EntityId == entityId && mt.Email == email);
    }

    public async Task<IReadOnlyList<IMessageTracker>> FindByEntityAsync(string entityType, int entityId)
    {
        return await _context.MessageTrackers
            .Where(mt => mt.EntityType == entityType && mt.EntityId == entityId)
            .ToListAsync();
    }
}

