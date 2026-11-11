using System;
using Guts.Common.Extensions;
using Guts.Domain.MessageTrackerAggregate;
using Guts.Domain.ValueObjects;

namespace Guts.Domain.Tests.Builders;

internal class MessageTrackerBuilder : BaseBuilder<MessageTracker>
{
    public MessageTrackerBuilder()
    {
        Item = new MessageTracker("PeerAssessment", Random.Shared.NextPositive(), new Email("student@pxl.be"), DateTime.UtcNow);
    }

    public MessageTrackerBuilder WithEntityType(string entityType)
    {
        SetProperty(mt => mt.EntityType, entityType);
        return this;
    }

    public MessageTrackerBuilder WithEntityId(int entityId)
    {
        SetProperty(mt => mt.EntityId, entityId);
        return this;
    }

    public MessageTrackerBuilder WithEmail(Email email)
    {
        SetProperty(mt => mt.Email, email);
        return this;
    }

    public MessageTrackerBuilder WithEmail(string email)
    {
        SetProperty(mt => mt.Email, new Email(email));
        return this;
    }

    public MessageTrackerBuilder WithStatus(MessageStatus status)
    {
        SetProperty(mt => mt.Status, status);
        return this;
    }

    public MessageTrackerBuilder WithCreatedAtUtc(DateTime createdAtUtc)
    {
        SetProperty(mt => mt.CreatedAtUtc, createdAtUtc);
        return this;
    }

    public MessageTrackerBuilder WithSentAtUtc(DateTime sentAtUtc)
    {
        SetProperty(mt => mt.SentAtUtc, sentAtUtc);
        return this;
    }

    public MessageTrackerBuilder WithRetryCount(int retryCount)
    {
        SetProperty(mt => mt.RetryCount, retryCount);
        return this;
    }

    public MessageTrackerBuilder WithFailureReason(string failureReason)
    {
        SetProperty(mt => mt.FailureReason, failureReason);
        return this;
    }
}
