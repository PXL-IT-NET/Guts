using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain.MessageTrackerAggregate;
using Guts.Domain.ValueObjects;

namespace Guts.Application.Repositories;

public interface IMessageTrackerRepository : IBasicRepository<IMessageTracker>
{
    Task<IMessageTracker> FindByEntityAndEmailAsync(string entityType, int entityId, Email email);
    Task<IReadOnlyList<IMessageTracker>> FindByEntityAsync(string entityType, int entityId);
}

