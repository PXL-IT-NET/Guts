using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Application.Dtos;
using Guts.Application.Repositories;
using Guts.Application.Services.Deletion;
using Guts.Application.Services.Period;
using Guts.Common.Extensions;
using Guts.Domain.DeletionJobAggregate;
using Guts.Domain.PeriodAggregate;
using Guts.Domain.Tests.Builders;
using Moq;
using NUnit.Framework;

namespace Guts.Application.Tests.Services;

[TestFixture]
public class PeriodServiceTests
{
    private PeriodService _service;
    private Mock<IPeriodFactory> _periodFactoryMock;
    private Mock<IPeriodRepository> _periodRepositoryMock;
    private Mock<IDeletionJobService> _deletionJobServiceMock;

    [SetUp]
    public void Setup()
    {
        _periodFactoryMock = new Mock<IPeriodFactory>();
        _periodRepositoryMock = new Mock<IPeriodRepository>();
        _deletionJobServiceMock = new Mock<IDeletionJobService>();

        _service = new PeriodService(_periodFactoryMock.Object, _periodRepositoryMock.Object, _deletionJobServiceMock.Object);
    }

    [Test]
    public async Task CreatePeriodAsync_ShouldCreateAndAddNewPeriod()
    {
        //Arrange
        string description = Random.Shared.NextString();
        DateTime from = Random.Shared.NextDateTimeInFuture();
        DateTime until = from.AddMonths(6);
        List<IPeriod> existingPeriods = [new PeriodBuilder().WithId().Build()];
        var createdPeriod = new PeriodBuilder().Build();
        var addedPeriod = new PeriodBuilder().WithId().Build();

        _periodRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(existingPeriods);
        _periodFactoryMock.Setup(factory => factory.CreateNew(description, from, until, existingPeriods))
            .Returns(createdPeriod);
        _periodRepositoryMock.Setup(repo => repo.AddAsync(createdPeriod)).ReturnsAsync(addedPeriod);

        //Act
        IPeriod result = await _service.CreatePeriodAsync(description, from, until);

        //Assert
        _periodRepositoryMock.Verify(repo => repo.GetAllAsync(), Times.Once);
        _periodFactoryMock.Verify(factory => factory.CreateNew(description, from, until, existingPeriods), Times.Once);
        _periodRepositoryMock.Verify(repo => repo.AddAsync(createdPeriod), Times.Once);
        Assert.That(result, Is.SameAs(addedPeriod));
    }

    [Test]
    public async Task UpdatePeriodAsync_ValidPeriod_ShouldUpdatePeriodInRepository()
    {
        //Arrange
        int periodId = Random.Shared.NextPositive();
        string newDescription = Random.Shared.NextString();
        DateTime newFrom = Random.Shared.NextDateTimeInFuture();
        DateTime newUntil = newFrom.AddMonths(6);
        List<IPeriod> existingPeriods = [new PeriodBuilder().WithId().Build()];
        var periodToUpdateMock = new Mock<IPeriod>();

        _periodRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(existingPeriods);
        _periodRepositoryMock.Setup(repo => repo.GetPeriodAsync(periodId)).ReturnsAsync(periodToUpdateMock.Object);

        //Act
        await _service.UpdatePeriodAsync(periodId, newDescription, newFrom, newUntil);

        //Assert
        _periodRepositoryMock.Verify(repo => repo.GetAllAsync(), Times.Once);
        _periodRepositoryMock.Verify(repo => repo.GetPeriodAsync(periodId), Times.Once);
        periodToUpdateMock.Verify(p => p.Update(newDescription, newFrom, newUntil, existingPeriods), Times.Once);
        _periodRepositoryMock.Verify(repo => repo.UpdateAsync(periodToUpdateMock.Object), Times.Once);
    }

    [Test]
    public void UpdatePeriodAsync_NonExistingPeriod_ShouldThrowDataNotFoundException()
    {
        //Arrange
        int periodId = Random.Shared.NextPositive();
        string newDescription = Random.Shared.NextString();
        DateTime newFrom = Random.Shared.NextDateTimeInFuture();
        DateTime newUntil = newFrom.AddMonths(6);
        var existingPeriods = new List<IPeriod>();

        _periodRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(existingPeriods);
        _periodRepositoryMock.Setup(repo => repo.GetPeriodAsync(periodId)).ThrowsAsync(new DataNotFoundException());

        //Act + Assert
        Assert.That(() => _service.UpdatePeriodAsync(periodId, newDescription, newFrom, newUntil),
            Throws.InstanceOf<DataNotFoundException>());
        _periodRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<IPeriod>()), Times.Never);
    }

    [Test]
    public async Task DeletePeriodAsync_PastPeriod_ShouldMarkAsDeletingAndQueueDeletionJob()
    {
        int periodId = Random.Shared.NextPositive();
        var periodMock = new Mock<IPeriod>();
        periodMock.Setup(p => p.Id).Returns(periodId);
        periodMock.Setup(p => p.Description).Returns("2020-2021");
        var job = new DeletionJobBuilder().WithTarget(DeletionTargetType.Period, periodId, "2020-2021").Build();
        var expectedDto = new DeletionJobDto { JobId = job.Id, TargetId = periodId };
        _periodRepositoryMock.Setup(repo => repo.GetPeriodAsync(periodId)).ReturnsAsync(periodMock.Object);
        _deletionJobServiceMock
            .Setup(s => s.QueueDeletionJobAsync(
                DeletionTargetType.Period,
                periodId,
                "2020-2021",
                It.IsAny<Func<DeletionJob, Task>>()))
            .Returns<DeletionTargetType, int, string, Func<DeletionJob, Task>>(async (_, _, _, persist) =>
            {
                await persist(job);
                return expectedDto;
            });

        var result = await _service.DeletePeriodAsync(periodId);

        periodMock.Verify(p => p.MarkAsDeleting(), Times.Once);
        _periodRepositoryMock.Verify(repo => repo.QueueDeletionAsync(periodMock.Object, job), Times.Once);
        Assert.That(result, Is.SameAs(expectedDto));
    }

    [Test]
    public void DeletePeriodAsync_WhenMarkAsDeletingThrows_ShouldNotQueueDeletion()
    {
        int periodId = Random.Shared.NextPositive();
        var periodMock = new Mock<IPeriod>();
        periodMock.Setup(p => p.MarkAsDeleting()).Throws(new Guts.Common.ContractException("Active"));
        _periodRepositoryMock.Setup(repo => repo.GetPeriodAsync(periodId)).ReturnsAsync(periodMock.Object);

        Assert.ThrowsAsync<Guts.Common.ContractException>(() => _service.DeletePeriodAsync(periodId));
        _periodRepositoryMock.Verify(repo => repo.QueueDeletionAsync(It.IsAny<IPeriod>(), It.IsAny<DeletionJob>()), Times.Never);
    }
}