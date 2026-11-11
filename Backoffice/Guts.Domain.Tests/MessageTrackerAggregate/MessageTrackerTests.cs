using System;
using Guts.Common;
using Guts.Domain.MessageTrackerAggregate;
using Guts.Domain.Tests.Builders;
using Guts.Domain.ValueObjects;
using NUnit.Framework;

namespace Guts.Domain.Tests.MessageTrackerAggregate;

[TestFixture]
public class MessageTrackerTests
{
    private DateTime _nowUtc;

    [SetUp]
    public void SetUp()
    {
        _nowUtc = DateTime.UtcNow;
    }

    [Test]
    public void Constructor_ValidArguments_ShouldInitializeCorrectly()
    {
        // Arrange
        string entityType = "PeerAssessment";
        int entityId = 42;
        Email email = new Email("student@pxl.be");

        // Act
        var tracker = new MessageTracker(entityType, entityId, email, _nowUtc);

        // Assert
        Assert.That(tracker.EntityType, Is.EqualTo(entityType));
        Assert.That(tracker.EntityId, Is.EqualTo(entityId));
        Assert.That(tracker.Email, Is.EqualTo(email));
        Assert.That(tracker.Status, Is.EqualTo(MessageStatus.Sending));
        Assert.That(tracker.CreatedAtUtc, Is.EqualTo(_nowUtc));
        Assert.That(tracker.SentAtUtc, Is.EqualTo(_nowUtc));
        Assert.That(tracker.RetryCount, Is.EqualTo(0));
        Assert.That(tracker.FailureReason, Is.Null);
        Assert.That(tracker.CanRetry, Is.False);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_InvalidEntityType_ShouldThrowContractException(string invalidEntityType)
    {
        Assert.Throws<ContractException>(() => new MessageTracker(invalidEntityType, 1, new Email("test@pxl.be"), _nowUtc));
    }

    [Test]
    public void Constructor_EntityTypeExceedsMaxLength_ShouldThrowContractException()
    {
        string tooLong = new string('a', MessageTracker.MaxEntityTypeLength + 1);
        Assert.Throws<ContractException>(() => new MessageTracker(tooLong, 1, new Email("test@pxl.be"), _nowUtc));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Constructor_InvalidEntityId_ShouldThrowContractException(int invalidEntityId)
    {
        Assert.Throws<ContractException>(() => new MessageTracker("PeerAssessment", invalidEntityId, new Email("test@pxl.be"), _nowUtc));
    }

    [Test]
    public void Constructor_NullEmail_ShouldThrowContractException()
    {
        Assert.Throws<ContractException>(() => new MessageTracker("PeerAssessment", 1, null, _nowUtc));
    }

    [Test]
    public void Constructor_NonUtcDateTime_ShouldThrowContractException()
    {
        var localTime = DateTime.Now;
        Assert.Throws<ContractException>(() => new MessageTracker("PeerAssessment", 1, new Email("test@pxl.be"), localTime));
    }

    [Test]
    public void MarkSucceeded_WhenStatusIsSending_ShouldSetStatusToSucceededAndClearFailureReason()
    {
        // Arrange
        var tracker = new MessageTrackerBuilder()
            .WithStatus(MessageStatus.Sending)
            .WithFailureReason("Previous failure")
            .Build();

        // Act
        tracker.MarkSucceeded();

        // Assert
        Assert.That(tracker.Status, Is.EqualTo(MessageStatus.Succeeded));
        Assert.That(tracker.FailureReason, Is.Null);
    }

    [TestCase(MessageStatus.Succeeded)]
    [TestCase(MessageStatus.Failed)]
    public void MarkSucceeded_WhenStatusIsNotSending_ShouldThrowContractException(MessageStatus nonSendingStatus)
    {
        // Arrange
        var tracker = new MessageTrackerBuilder()
            .WithStatus(nonSendingStatus)
            .Build();

        // Act & Assert
        Assert.Throws<ContractException>(() => tracker.MarkSucceeded());
    }

    [Test]
    public void MarkFailed_WhenStatusIsSending_ShouldSetStatusToFailedIncrementRetryCountAndSetReason()
    {
        // Arrange
        var tracker = new MessageTrackerBuilder()
            .WithStatus(MessageStatus.Sending)
            .WithRetryCount(1)
            .Build();
        string reason = "SMTP connection timed out";

        // Act
        tracker.MarkFailed(reason);

        // Assert
        Assert.That(tracker.Status, Is.EqualTo(MessageStatus.Failed));
        Assert.That(tracker.RetryCount, Is.EqualTo(2));
        Assert.That(tracker.FailureReason, Is.EqualTo(reason));
    }

    [TestCase(MessageStatus.Succeeded)]
    [TestCase(MessageStatus.Failed)]
    public void MarkFailed_WhenStatusIsNotSending_ShouldThrowContractException(MessageStatus nonSendingStatus)
    {
        // Arrange
        var tracker = new MessageTrackerBuilder()
            .WithStatus(nonSendingStatus)
            .Build();

        // Act & Assert
        Assert.Throws<ContractException>(() => tracker.MarkFailed("Some failure"));
    }

    [Test]
    public void Retry_WhenCanRetryAndUtcDate_ShouldSetStatusToSendingUpdateSentAtUtcAndClearFailureReason()
    {
        // Arrange
        var tracker = new MessageTrackerBuilder()
            .WithStatus(MessageStatus.Failed)
            .WithRetryCount(1)
            .WithFailureReason("Previous error")
            .Build();

        var retryUtc = _nowUtc.AddMinutes(5);

        // Act
        tracker.Retry(retryUtc);

        // Assert
        Assert.That(tracker.Status, Is.EqualTo(MessageStatus.Sending));
        Assert.That(tracker.SentAtUtc, Is.EqualTo(retryUtc));
        Assert.That(tracker.FailureReason, Is.Null);
        Assert.That(tracker.RetryCount, Is.EqualTo(1));
    }

    [Test]
    public void Retry_WhenStatusIsNotFailed_ShouldThrowContractException()
    {
        // Arrange
        var tracker = new MessageTrackerBuilder()
            .WithStatus(MessageStatus.Sending)
            .WithRetryCount(0)
            .Build();

        // Act & Assert
        Assert.Throws<ContractException>(() => tracker.Retry(_nowUtc));
    }

    [Test]
    public void Retry_WhenRetryCountIsThreeOrMore_ShouldThrowContractException()
    {
        // Arrange
        var tracker = new MessageTrackerBuilder()
            .WithStatus(MessageStatus.Failed)
            .WithRetryCount(3)
            .Build();

        // Act & Assert
        Assert.Throws<ContractException>(() => tracker.Retry(_nowUtc));
    }

    [Test]
    public void Retry_WhenSentAtUtcIsNotUtc_ShouldThrowContractException()
    {
        // Arrange
        var tracker = new MessageTrackerBuilder()
            .WithStatus(MessageStatus.Failed)
            .WithRetryCount(1)
            .Build();

        // Act & Assert
        Assert.Throws<ContractException>(() => tracker.Retry(DateTime.Now));
    }

    [TestCase(MessageStatus.Sending, 0, false)]
    [TestCase(MessageStatus.Sending, 1, false)]
    [TestCase(MessageStatus.Succeeded, 0, false)]
    [TestCase(MessageStatus.Succeeded, 1, false)]
    [TestCase(MessageStatus.Failed, 0, true)]
    [TestCase(MessageStatus.Failed, 1, true)]
    [TestCase(MessageStatus.Failed, 2, true)]
    [TestCase(MessageStatus.Failed, 3, false)]
    [TestCase(MessageStatus.Failed, 4, false)]
    public void CanRetry_ShouldEvaluateCorrectly(MessageStatus status, int retryCount, bool expectedCanRetry)
    {
        // Arrange
        var tracker = new MessageTrackerBuilder()
            .WithStatus(status)
            .WithRetryCount(retryCount)
            .Build();

        // Assert
        Assert.That(tracker.CanRetry, Is.EqualTo(expectedCanRetry));
    }

    [Test]
    public void Equals_MatchingEntityTypeEntityIdAndEmail_ShouldBeEqual()
    {
        var email = new Email("student@pxl.be");
        var trackerA = new MessageTracker("PeerAssessment", 10, email, _nowUtc);
        var trackerB = new MessageTracker("PeerAssessment", 10, email, _nowUtc);

        Assert.That(trackerA.Equals(trackerB), Is.True);
        Assert.That(trackerA == trackerB, Is.True);
        Assert.That(trackerA != trackerB, Is.False);
        Assert.That(trackerA.GetHashCode(), Is.EqualTo(trackerB.GetHashCode()));
    }

    [Test]
    public void Equals_DifferingKeyComponents_ShouldNotBeEqual()
    {
        var email1 = new Email("student1@pxl.be");
        var email2 = new Email("student2@pxl.be");
        var tracker1 = new MessageTracker("PeerAssessment", 10, email1, _nowUtc);
        var tracker2 = new MessageTracker("PeerAssessment", 10, email2, _nowUtc);
        var tracker3 = new MessageTracker("PeerAssessment", 11, email1, _nowUtc);
        var tracker4 = new MessageTracker("OtherType", 10, email1, _nowUtc);

        Assert.That(tracker1.Equals(tracker2), Is.False);
        Assert.That(tracker1.Equals(tracker3), Is.False);
        Assert.That(tracker1.Equals(tracker4), Is.False);
        Assert.That(tracker1.GetHashCode(), Is.Not.EqualTo(tracker2.GetHashCode()));
    }
}
