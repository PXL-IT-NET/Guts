using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Guts.Common;
using Guts.Common.Extensions;
using Guts.Domain.PeriodAggregate;
using Guts.Domain.Tests.Builders;
using NUnit.Framework;

namespace Guts.Domain.Tests.PeriodAggregate;

[TestFixture]
public class PeriodTests
{
    private Period.Factory _factory;
    private IReadOnlyList<Period> _existingPeriods;

    [SetUp]
    public void SetUp()
    {
        _factory = new Period.Factory();
        _existingPeriods = new List<Period>
        {
            new PeriodBuilder().WithId().WithRange(DateTime.Now.AddMonths(-12), DateTime.Now.AddMonths(-6)).Build(),
            new PeriodBuilder().WithId().WithRange(DateTime.Now.AddMonths(-6), DateTime.Now).Build(),
        };
    }

    [Test]
    public void Factory_CreateNew_ValidPeriod_ShouldConstructPeriodCorrectly()
    {
        // Arrange
        string description = Random.Shared.NextString();
        DateTime from = DateTime.Now;
        DateTime until = DateTime.Now.AddMonths(6);

        // Act
        Period newPeriod = _factory.CreateNew(description, from, until, _existingPeriods);

        // Assert
        Assert.That(newPeriod.Id, Is.Zero);
        Assert.That(newPeriod.Description, Is.EqualTo(description));
        Assert.That(newPeriod.From, Is.EqualTo(from));
        Assert.That(newPeriod.Until, Is.EqualTo(until));
    }

    [Test]
    public void Factory_CreateNew_HasOverlapWithExistingPeriod_ShouldThrowContractException()
    {
        // Arrange
        string description = Random.Shared.NextString();
        DateTime from = DateTime.Now.AddMonths(-1);
        DateTime until = DateTime.Now.AddMonths(5);

        // Act & Assert
        Assert.Throws<ContractException>(() => _factory.CreateNew(description, from, until, _existingPeriods));
    }

    [Test]
    public void Factory_CreateNew_UntilIsBeforeFrom_ShouldThrowContractException()
    {
        // Arrange
        string description = Random.Shared.NextString();
        DateTime from = DateTime.Now.AddMonths(5);
        DateTime until = DateTime.Now.AddMonths(1);

        // Act & Assert
        Assert.Throws<ContractException>(() => _factory.CreateNew(description, from, until, _existingPeriods));
    }

    [Test]
    public void DescriptionSetter_EmptyDescription_ShouldThrowContractException()
    {
        // Arrange
        var period = new PeriodBuilder().WithId().Build();

        // Act & Assert
        Assert.That(() => period.Description = null, Throws.InstanceOf<ContractException>());
        Assert.That(() => period.Description = string.Empty, Throws.InstanceOf<ContractException>());
    }

    [TestCase("2023-01-01", "2023-06-01", "2023-05-01", "2023-07-01", ExpectedResult = true)]
    [TestCase("2023-01-01", "2023-06-01", "2022-12-01", "2023-02-01", ExpectedResult = true)]
    [TestCase("2023-01-01", "2023-06-01", "2023-01-01", "2023-06-01", ExpectedResult = true)]
    [TestCase("2023-01-01", "2023-06-01", "2023-07-01", "2023-12-01", ExpectedResult = false)]
    [TestCase("2023-01-01", "2023-06-01", "2022-07-01", "2022-12-01", ExpectedResult = false)]
    public bool OverlapsWithPeriod_ShouldReturnExpectedResult(string periodFrom, string periodUntil, string testFrom, string testUntil)
    {
        // Arrange
        Period period = new PeriodBuilder().WithId().WithRange(DateTime.Parse(periodFrom, CultureInfo.InvariantCulture), DateTime.Parse(periodUntil, CultureInfo.InvariantCulture)).Build();

        // Act
        bool result = period.OverlapsWith(DateTime.Parse(testFrom, CultureInfo.InvariantCulture), DateTime.Parse(testUntil, CultureInfo.InvariantCulture));

        // Assert
        return result;
    }
    [Test]
    public void Update_ValidPeriod_ShouldUpdatePeriodCorrectly()
    {
        // Arrange
        Period period = _existingPeriods[1];
        string newDescription = Random.Shared.NextString();
        DateTime newFrom = DateTime.Now.AddMonths(-5);
        DateTime newUntil = DateTime.Now.AddMonths(1);

        // Act
        period.Update(newDescription, newFrom, newUntil, _existingPeriods);

        // Assert
        Assert.That(period.Description, Is.EqualTo(newDescription));
        Assert.That(period.From, Is.EqualTo(newFrom));
        Assert.That(period.Until, Is.EqualTo(newUntil));
    }

    [Test]
    public void Update_HasOverlapWithExistingPeriod_ShouldThrowContractException()
    {
        // Arrange
        Period period = _existingPeriods[0];
        string newDescription = Random.Shared.NextString();
        DateTime newFrom = DateTime.Now.AddMonths(-10);
        DateTime newUntil = DateTime.Now.AddMonths(-3);

        // Act & Assert
        Assert.Throws<ContractException>(() => period.Update(newDescription, newFrom, newUntil, _existingPeriods));
    }

    [Test]
    public void Update_UntilIsBeforeFrom_ShouldThrowContractException()
    {
        // Arrange
        Period period = _existingPeriods[0];
        string newDescription = Random.Shared.NextString();
        DateTime newFrom = DateTime.Now.AddMonths(-7);
        DateTime newUntil = DateTime.Now.AddMonths(-10);

        // Act & Assert
        Assert.Throws<ContractException>(() => period.Update(newDescription, newFrom, newUntil, _existingPeriods));
    }

    [Test]
    public void Update_NewDatesOverlapWithSelfInExistingPeriods_ShouldUpdateWithoutThrowing()
    {
        // Arrange
        Period period = _existingPeriods[1];
        string newDescription = Random.Shared.NextString();
        DateTime newFrom = period.From.AddDays(1);
        DateTime newUntil = period.Until.AddDays(1);

        // Act
        period.Update(newDescription, newFrom, newUntil, _existingPeriods);

        // Assert
        Assert.That(period.Description, Is.EqualTo(newDescription));
        Assert.That(period.From, Is.EqualTo(newFrom));
        Assert.That(period.Until, Is.EqualTo(newUntil));
    }

    [Test]
    public void Update_InactivePeriodWithUnchangedDates_ShouldUpdateDescription()
    {
        Period period = _existingPeriods[0];
        string newDescription = Random.Shared.NextString();

        period.Update(newDescription, period.From, period.Until, _existingPeriods);

        Assert.That(period.Description, Is.EqualTo(newDescription));
    }

    [Test]
    public void Update_InactivePeriodWithChangedFromDate_ShouldThrowContractException()
    {
        Period period = _existingPeriods[0];
        string newDescription = Random.Shared.NextString();

        Assert.Throws<ContractException>(() =>
            period.Update(newDescription, period.From.AddDays(1), period.Until, _existingPeriods));
    }

    [Test]
    public void Update_InactivePeriodWithChangedUntilDate_ShouldThrowContractException()
    {
        Period period = _existingPeriods[0];
        string newDescription = Random.Shared.NextString();

        Assert.Throws<ContractException>(() =>
            period.Update(newDescription, period.From, period.Until.AddDays(-1), _existingPeriods));
    }

    [Test]
    public void IsActive_TodayIsWithinRange_ShouldReturnTrue()
    {
        // Arrange
        Period activePeriod = new PeriodBuilder().WithId().WithRange(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1)).Build();
        Period boundaryPeriod = new PeriodBuilder().WithId().WithRange(DateTime.Today, DateTime.Today.AddDays(1)).Build();

        // Act & Assert
        Assert.That(activePeriod.IsActive, Is.True);
        Assert.That(boundaryPeriod.IsActive, Is.True);
    }

    [Test]
    public void IsActive_TodayIsOutsideRange_ShouldReturnFalse()
    {
        // Arrange
        Period pastPeriod = new PeriodBuilder().WithId().WithRange(DateTime.Today.AddDays(-10), DateTime.Today.AddDays(-1)).Build();
        Period futurePeriod = new PeriodBuilder().WithId().WithRange(DateTime.Today.AddDays(1), DateTime.Today.AddDays(10)).Build();

        // Act & Assert
        Assert.That(pastPeriod.IsActive, Is.False);
        Assert.That(futurePeriod.IsActive, Is.False);
    }

    [Test]
    public void EnsureCanBeDeleted_PastPeriod_ShouldNotThrow()
    {
        Period pastPeriod = new PeriodBuilder().WithId().WithRange(DateTime.Today.AddDays(-10), DateTime.Today.AddDays(-1)).Build();

        Assert.DoesNotThrow(() => pastPeriod.EnsureCanBeDeleted());
    }

    [Test]
    public void EnsureCanBeDeleted_ActivePeriod_ShouldThrowContractException()
    {
        Period activePeriod = new PeriodBuilder().WithId().WithRange(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1)).Build();

        Assert.Throws<ContractException>(() => activePeriod.EnsureCanBeDeleted());
    }

    [Test]
    public void EnsureCanBeDeleted_FuturePeriod_ShouldNotThrow()
    {
        Period futurePeriod = new PeriodBuilder().WithId().WithRange(DateTime.Today.AddDays(1), DateTime.Today.AddDays(10)).Build();

        Assert.DoesNotThrow(() => futurePeriod.EnsureCanBeDeleted());
    }
}