using System;
using Guts.Domain.ValueObjects;

namespace Guts.Domain.MessageTrackerAggregate;

public interface IMessageTracker : IEntity
{
    string EntityType { get; }
    int EntityId { get; }
    Email Email { get; }
    MessageStatus Status { get; }
    DateTime CreatedAtUtc { get; }
    DateTime SentAtUtc { get; }
    int RetryCount { get; }
    string FailureReason { get; }
    bool CanRetry { get; }

    void MarkSucceeded();
    void MarkFailed(string reason);
    void Retry(DateTime sentAtUtc);
}
