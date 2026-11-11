using System;
using Guts.Domain.MessageTrackerAggregate;
using Guts.Domain.ValueObjects;
using NUnit.Framework;

namespace Guts.Domain.Tests.MessageTrackerAggregate;

[TestFixture]
public class MessageTrackerFactoryTests
{
    private MessageTrackerFactory _factory;

    [SetUp]
    public void SetUp()
    {
        _factory = new MessageTrackerFactory();
    }

    [Test]
    public void CreateNew_ValidArguments_ShouldReturnInitializedMessageTracker()
    {
        // Arrange
        string entityType = "PeerAssessment";
        int entityId = 15;
        Email email = new Email("test@pxl.be");
        var nowUtc = DateTime.UtcNow;

        // Act
        var tracker = _factory.CreateNew(entityType, entityId, email, nowUtc);

        // Assert
        Assert.That(tracker, Is.Not.Null);
        Assert.That(tracker.EntityType, Is.EqualTo(entityType));
        Assert.That(tracker.EntityId, Is.EqualTo(entityId));
        Assert.That(tracker.Email, Is.EqualTo(email));
        Assert.That(tracker.Status, Is.EqualTo(MessageStatus.Sending));
        Assert.That(tracker.CreatedAtUtc, Is.EqualTo(nowUtc));
        Assert.That(tracker.SentAtUtc, Is.EqualTo(nowUtc));
        Assert.That(tracker.RetryCount, Is.EqualTo(0));
        Assert.That(tracker.FailureReason, Is.Null);
    }
}
