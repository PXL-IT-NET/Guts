using System;
using System.Collections.Generic;
using Guts.Common;
using Guts.Domain.ValueObjects;

namespace Guts.Domain.MessageTrackerAggregate;

public class MessageTracker : Entity, IMessageTracker
{
    public const int MaxEntityTypeLength = 64;

    public string EntityType { get; private set; }
    public int EntityId { get; private set; }
    public Email Email { get; private set; }
    public MessageStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime SentAtUtc { get; private set; }
    public int RetryCount { get; private set; }
    public string FailureReason { get; private set; }

    public bool CanRetry => Status == MessageStatus.Failed && RetryCount < 3;

    private MessageTracker() { } // Needed for EF

    internal MessageTracker(string entityType, int entityId, Email email, DateTime nowUtc)
    {
        Contracts.Require(!string.IsNullOrWhiteSpace(entityType), "The entity type of a message tracker cannot be empty.");
        string trimmedEntityType = entityType.Trim();
        Contracts.Require(trimmedEntityType.Length <= MaxEntityTypeLength, $"The entity type cannot exceed {MaxEntityTypeLength} characters.");
        Contracts.Require(entityId > 0, "The entity id of a message tracker must be positive.");
        Contracts.Require(email != null, "The email of a message tracker cannot be null.");
        Contracts.Require(nowUtc.Kind == DateTimeKind.Utc, "The creation timestamp must be a UTC date.");

        EntityType = trimmedEntityType;
        EntityId = entityId;
        Email = email;
        Status = MessageStatus.Sending;
        CreatedAtUtc = nowUtc;
        SentAtUtc = nowUtc;
        RetryCount = 0;
        FailureReason = null;
    }

    public void MarkSucceeded()
    {
        Contracts.Require(Status == MessageStatus.Sending, "MarkSucceeded can only follow a Sending status.");
        Status = MessageStatus.Succeeded;
        FailureReason = null;
    }

    public void MarkFailed(string reason)
    {
        Contracts.Require(Status == MessageStatus.Sending, "MarkFailed can only follow a Sending status.");
        Status = MessageStatus.Failed;
        RetryCount++;
        FailureReason = reason;
    }

    public void Retry(DateTime sentAtUtc)
    {
        Contracts.Require(CanRetry, "Cannot retry this message because it is not in a failed state or the maximum retry count has been reached.");
        Contracts.Require(sentAtUtc.Kind == DateTimeKind.Utc, "The retry timestamp must be a UTC date.");

        Status = MessageStatus.Sending;
        SentAtUtc = sentAtUtc;
        FailureReason = null;
    }

    protected override IEnumerable<object> GetIdComponents()
    {
        yield return EntityType;
        yield return EntityId;
        yield return Email;
    }
}
