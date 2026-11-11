using System;
using Guts.Domain.ValueObjects;

namespace Guts.Domain.MessageTrackerAggregate;

public interface IMessageTrackerFactory
{
    IMessageTracker CreateNew(string entityType, int entityId, Email email, DateTime nowUtc);
}
