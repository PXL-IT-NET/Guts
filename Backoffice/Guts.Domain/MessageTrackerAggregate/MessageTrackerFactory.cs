using System;
using Guts.Domain.ValueObjects;

namespace Guts.Domain.MessageTrackerAggregate;

internal class MessageTrackerFactory : IMessageTrackerFactory
{
    public IMessageTracker CreateNew(string entityType, int entityId, Email email, DateTime nowUtc)
    {
        return new MessageTracker(entityType, entityId, email, nowUtc);
    }
}
