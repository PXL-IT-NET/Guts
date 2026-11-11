using System;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Moq;
using NUnit.Framework;

namespace Guts.Domain.Tests.TopicAggregate.ProjectAggregate;

[TestFixture]
public class PeerAssessmentReminderPolicyTests
{
    private DateTime _nowUtc;
    private Mock<IProjectAssessment> _assessmentMock;

    [SetUp]
    public void SetUp()
    {
        _nowUtc = new DateTime(2026, 11, 11, 12, 0, 0, DateTimeKind.Utc);
        _assessmentMock = new Mock<IProjectAssessment>();
    }

    [Test]
    public void IsAssessmentEligibleForReminders_WhenNull_ShouldReturnFalse()
    {
        Assert.That(PeerAssessmentReminderPolicy.IsAssessmentEligibleForReminders(null, _nowUtc), Is.False);
    }

    [Test]
    public void IsAssessmentEligibleForReminders_WhenOpenAndDurationAtLeast48HoursAndWithinFinal24Hours_ShouldReturnTrue()
    {
        // Total window: 72 hours (3 days), currently 12 hours before deadline
        var openOn = _nowUtc.AddHours(-60);
        var deadline = _nowUtc.AddHours(12);

        _assessmentMock.SetupGet(a => a.OpenOnUtc).Returns(openOn);
        _assessmentMock.SetupGet(a => a.DeadlineUtc).Returns(deadline);

        bool eligible = PeerAssessmentReminderPolicy.IsAssessmentEligibleForReminders(_assessmentMock.Object, _nowUtc);

        Assert.That(eligible, Is.True);
    }

    [Test]
    public void IsAssessmentEligibleForReminders_ExactBoundary_48HoursTotalAnd24HoursRemaining_ShouldReturnTrue()
    {
        // Total window: exactly 48 hours, remaining: exactly 24 hours
        var openOn = _nowUtc.AddHours(-24);
        var deadline = _nowUtc.AddHours(24);

        _assessmentMock.SetupGet(a => a.OpenOnUtc).Returns(openOn);
        _assessmentMock.SetupGet(a => a.DeadlineUtc).Returns(deadline);

        bool eligible = PeerAssessmentReminderPolicy.IsAssessmentEligibleForReminders(_assessmentMock.Object, _nowUtc);

        Assert.That(eligible, Is.True);
    }

    [Test]
    public void IsAssessmentEligibleForReminders_WhenNotYetOpen_ShouldReturnFalse()
    {
        // Opens in 2 hours
        var openOn = _nowUtc.AddHours(2);
        var deadline = _nowUtc.AddHours(50);

        _assessmentMock.SetupGet(a => a.OpenOnUtc).Returns(openOn);
        _assessmentMock.SetupGet(a => a.DeadlineUtc).Returns(deadline);

        bool eligible = PeerAssessmentReminderPolicy.IsAssessmentEligibleForReminders(_assessmentMock.Object, _nowUtc);

        Assert.That(eligible, Is.False);
    }

    [Test]
    public void IsAssessmentEligibleForReminders_WhenDeadlinePassed_ShouldReturnFalse()
    {
        // Deadline was 1 hour ago
        var openOn = _nowUtc.AddHours(-72);
        var deadline = _nowUtc.AddHours(-1);

        _assessmentMock.SetupGet(a => a.OpenOnUtc).Returns(openOn);
        _assessmentMock.SetupGet(a => a.DeadlineUtc).Returns(deadline);

        bool eligible = PeerAssessmentReminderPolicy.IsAssessmentEligibleForReminders(_assessmentMock.Object, _nowUtc);

        Assert.That(eligible, Is.False);
    }

    [Test]
    public void IsAssessmentEligibleForReminders_WhenTotalDurationLessThan48Hours_ShouldReturnFalse()
    {
        // Total window is 47 hours and 59 minutes (less than 48h)
        var openOn = _nowUtc.AddHours(-36);
        var deadline = _nowUtc.AddHours(11).AddMinutes(59);

        _assessmentMock.SetupGet(a => a.OpenOnUtc).Returns(openOn);
        _assessmentMock.SetupGet(a => a.DeadlineUtc).Returns(deadline);

        bool eligible = PeerAssessmentReminderPolicy.IsAssessmentEligibleForReminders(_assessmentMock.Object, _nowUtc);

        Assert.That(eligible, Is.False);
    }

    [Test]
    public void IsAssessmentEligibleForReminders_WhenMoreThan24HoursRemaining_ShouldReturnFalse()
    {
        // Total window 72 hours, but remaining time is 25 hours
        var openOn = _nowUtc.AddHours(-47);
        var deadline = _nowUtc.AddHours(25);

        _assessmentMock.SetupGet(a => a.OpenOnUtc).Returns(openOn);
        _assessmentMock.SetupGet(a => a.DeadlineUtc).Returns(deadline);

        bool eligible = PeerAssessmentReminderPolicy.IsAssessmentEligibleForReminders(_assessmentMock.Object, _nowUtc);

        Assert.That(eligible, Is.False);
    }

    [Test]
    public void IsAssessmentEligibleForReminders_WhenJustBeforeDeadline_ShouldReturnTrue()
    {
        // Total window 5 days, 1 minute before deadline
        var openOn = _nowUtc.AddDays(-5);
        var deadline = _nowUtc.AddMinutes(1);

        _assessmentMock.SetupGet(a => a.OpenOnUtc).Returns(openOn);
        _assessmentMock.SetupGet(a => a.DeadlineUtc).Returns(deadline);

        bool eligible = PeerAssessmentReminderPolicy.IsAssessmentEligibleForReminders(_assessmentMock.Object, _nowUtc);

        Assert.That(eligible, Is.True);
    }
}

