using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Api.Controllers;
using Guts.Api.Models;
using Guts.Api.Models.PeriodModels;
using Guts.Business;
using Guts.Business.Repositories;
using Guts.Business.Services.Period;
using Guts.Common;
using Guts.Common.Extensions;
using Guts.Domain.PeriodAggregate;
using Guts.Domain.Tests.Builders;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using ControllerBase = Guts.Api.Controllers.ControllerBase;

namespace Guts.Api.Tests.Controllers;

[TestFixture]
public class PeriodControllerTests
{
    private PeriodController _controller;
    private Mock<IPeriodRepository> _periodRepositoryMock;
    private Mock<IPeriodService> _periodServiceMock;
    private Mock<IMapper> _mapperMock;

    [SetUp]
    public void Setup()
    {
        _periodRepositoryMock = new Mock<IPeriodRepository>();
        _periodServiceMock = new Mock<IPeriodService>();
        _mapperMock = new Mock<IMapper>();

        _controller = new PeriodController(
            _periodRepositoryMock.Object,
            _periodServiceMock.Object,
            _mapperMock.Object);
    }

    [Test]
    public void ShouldInheritFromControllerBase()
    {
        Assert.That(_controller, Is.InstanceOf<ControllerBase>());
    }

    [Test]
    public async Task GetAll_ShouldReturnOkWithMappedPeriods()
    {
        // Arrange
        var periods = new List<IPeriod>
        {
            new PeriodBuilder().WithId().Build(),
            new PeriodBuilder().WithId().Build()
        };
        _periodRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(periods);

        var mappedModel = new PeriodOutputModel();
        _mapperMock.Setup(mapper => mapper.MapToPeriodOutputModel(It.IsAny<IPeriod>())).Returns(mappedModel);

        // Act
        var result = await _controller.GetAll() as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        var returnedModels = result.Value as IList<PeriodOutputModel>;
        Assert.That(returnedModels, Is.Not.Null);
        Assert.That(returnedModels, Has.Count.EqualTo(periods.Count));
        foreach (var period in periods)
        {
            _mapperMock.Verify(mapper => mapper.MapToPeriodOutputModel(period), Times.Once);
        }
    }

    [Test]
    public async Task GetById_PeriodExists_ShouldReturnOkWithMappedPeriod()
    {
        // Arrange
        IPeriod period = new PeriodBuilder().WithId().Build();
        _periodRepositoryMock.Setup(repo => repo.GetPeriodAsync(period.Id)).ReturnsAsync(period);

        var mappedModel = new PeriodOutputModel { Id = period.Id };
        _mapperMock.Setup(mapper => mapper.MapToPeriodOutputModel(period)).Returns(mappedModel);

        // Act
        var result = await _controller.GetById(period.Id) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Is.SameAs(mappedModel));
        _periodRepositoryMock.Verify(repo => repo.GetPeriodAsync(period.Id), Times.Once);
        _mapperMock.Verify(mapper => mapper.MapToPeriodOutputModel(period), Times.Once);
    }

    [Test]
    public async Task GetById_PeriodDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        int periodId = Random.Shared.NextPositive();
        _periodRepositoryMock.Setup(repo => repo.GetPeriodAsync(periodId)).ThrowsAsync(new DataNotFoundException());

        // Act
        var result = await _controller.GetById(periodId) as NotFoundResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        _periodRepositoryMock.Verify(repo => repo.GetPeriodAsync(periodId), Times.Once);
        _mapperMock.Verify(mapper => mapper.MapToPeriodOutputModel(It.IsAny<IPeriod>()), Times.Never);
    }

    [Test]
    public async Task CreatePeriod_ValidModel_ShouldCreatePeriodAndReturnCreatedAtAction()
    {
        // Arrange
        var creationModel = new PeriodCreationModel
        {
            Description = Random.Shared.NextString(),
            From = Random.Shared.NextDateTimeInPast(),
            Until = Random.Shared.NextDateTimeInFuture()
        };

        IPeriod createdPeriod = new PeriodBuilder().WithId().Build();
        _periodServiceMock
            .Setup(service => service.CreatePeriodAsync(creationModel.Description, creationModel.From, creationModel.Until))
            .ReturnsAsync(createdPeriod);

        var outputModel = new PeriodOutputModel { Id = createdPeriod.Id };
        _mapperMock.Setup(mapper => mapper.MapToPeriodOutputModel(createdPeriod)).Returns(outputModel);

        // Act
        var result = await _controller.CreatePeriod(creationModel) as CreatedAtActionResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ActionName, Is.EqualTo(nameof(PeriodController.GetById)));
        Assert.That(result.RouteValues["id"], Is.EqualTo(outputModel.Id));
        Assert.That(result.Value, Is.SameAs(outputModel));
        _periodServiceMock.Verify(
            service => service.CreatePeriodAsync(creationModel.Description, creationModel.From, creationModel.Until),
            Times.Once);
        _mapperMock.Verify(mapper => mapper.MapToPeriodOutputModel(createdPeriod), Times.Once);
    }

    [Test]
    public async Task CreatePeriod_WhenContractExceptionThrown_ShouldReturnBadRequest()
    {
        // Arrange
        var creationModel = new PeriodCreationModel
        {
            Description = string.Empty,
            From = DateTime.UtcNow,
            Until = DateTime.UtcNow.AddDays(-1)
        };

        _periodServiceMock
            .Setup(service => service.CreatePeriodAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ThrowsAsync(new ContractException("Invalid period"));

        // Act
        var result = await _controller.CreatePeriod(creationModel) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Is.InstanceOf<ErrorModel>());
    }
}
