using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Threading.Tasks;
using Guts.Api.Controllers;
using Guts.Api.Models;
using Guts.Api.Models.PeriodModels;
using Guts.Application;
using Guts.Application.Repositories;
using Guts.Application.Services.Period;
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
        _periodRepositoryMock = new();
        _periodServiceMock = new();
        _mapperMock = new();

        _controller = new(
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
            .Setup(service => service.CreatePeriodAsync(creationModel.Description, creationModel.From.Value, creationModel.Until.Value))
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
            service => service.CreatePeriodAsync(creationModel.Description, creationModel.From.Value, creationModel.Until.Value),
            Times.Once);
        _mapperMock.Verify(mapper => mapper.MapToPeriodOutputModel(createdPeriod), Times.Once);
    }

    [Test]
    public async Task CreatePeriod_NullModel_ShouldReturnBadRequest()
    {
        var result = await _controller.CreatePeriod(null) as BadRequestObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Is.InstanceOf<ErrorModel>());
        _periodServiceMock.Verify(
            service => service.CreatePeriodAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()),
            Times.Never);
    }

    [Test]
    public async Task CreatePeriod_InvalidModelState_ShouldReturnBadRequest()
    {
        var creationModel = new PeriodCreationModel
        {
            Description = "2026-2027",
            From = null,
            Until = null
        };
        _controller.ModelState.AddModelError(nameof(PeriodCreationModel.From), "The From field is required.");

        var result = await _controller.CreatePeriod(creationModel) as BadRequestObjectResult;

        Assert.That(result, Is.Not.Null);
        _periodServiceMock.Verify(
            service => service.CreatePeriodAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()),
            Times.Never);
    }

    [Test]
    public void PeriodCreationModel_EmptyDateStrings_ShouldDeserializeToNonNullModelWithNullDatesAndFailValidation()
    {
        const string json = """{"description":"2026-2027","from":"","until":""}""";

        var model = JsonSerializer.Deserialize<PeriodCreationModel>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.That(model, Is.Not.Null);
        Assert.That(model.Description, Is.EqualTo("2026-2027"));
        Assert.That(model.From, Is.Null);
        Assert.That(model.Until, Is.Null);

        var validationResults = new List<ValidationResult>();
        bool isValid = Validator.TryValidateObject(model, new ValidationContext(model), validationResults, validateAllProperties: true);
        Assert.That(isValid, Is.False);
        Assert.That(validationResults, Has.Count.EqualTo(2));
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

    [TestCase(nameof(PeriodController.CreatePeriod))]
    [TestCase(nameof(PeriodController.UpdatePeriod))]
    [TestCase(nameof(PeriodController.DeletePeriod))]
    public void MutationEndpoints_ShouldHaveAdminOnlyPolicy(string methodName)
    {
        var method = typeof(PeriodController).GetMethod(methodName);
        var attr = method?.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true)[0]
            as Microsoft.AspNetCore.Authorization.AuthorizeAttribute;
        Assert.That(attr, Is.Not.Null);
        Assert.That(attr.Policy, Is.EqualTo(ApiConstants.AdminOnlyPolicy));
    }

    [Test]
    public async Task UpdatePeriod_Valid_ReturnsOk()
    {
        var model = new PeriodCreationModel { Description = "2026-2027", From = DateTime.Today, Until = DateTime.Today.AddMonths(6) };

        var result = await _controller.UpdatePeriod(1, model);

        Assert.That(result, Is.InstanceOf<OkResult>());
        _periodServiceMock.Verify(s => s.UpdatePeriodAsync(1, model.Description, model.From.Value, model.Until.Value), Times.Once);
    }

    [Test]
    public async Task DeletePeriod_Valid_ReturnsAcceptedWithStatusModel()
    {
        var dto = new Guts.Application.Dtos.DeletionJobDto { JobId = Guid.NewGuid(), TargetId = 1 };
        var model = new Guts.Api.Models.DeletionJobStatusModel { JobId = dto.JobId, TargetId = 1, Status = "Pending" };
        _periodServiceMock.Setup(s => s.DeletePeriodAsync(1)).ReturnsAsync(dto);
        _mapperMock.Setup(m => m.MapToDeletionJobStatusModel(dto)).Returns(model);

        var result = await _controller.DeletePeriod(1) as AcceptedResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value, Is.SameAs(model));
        _periodServiceMock.Verify(s => s.DeletePeriodAsync(1), Times.Once);
    }

    [Test]
    public async Task DeletePeriod_ContractException_ReturnsBadRequest()
    {
        _periodServiceMock.Setup(s => s.DeletePeriodAsync(1)).ThrowsAsync(new ContractException("Active"));

        var result = await _controller.DeletePeriod(1);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreateAndUpdateAndDelete_InvalidOrNotFoundCases_ReturnExpectedResponses()
    {
        Assert.That(await _controller.CreatePeriod(null), Is.InstanceOf<BadRequestObjectResult>());
        Assert.That(await _controller.UpdatePeriod(1, null), Is.InstanceOf<BadRequestObjectResult>());

        var missingDates = new PeriodCreationModel { Description = "2026-2027", From = null, Until = null };
        Assert.That(await _controller.CreatePeriod(missingDates), Is.InstanceOf<BadRequestObjectResult>());
        Assert.That(await _controller.UpdatePeriod(1, missingDates), Is.InstanceOf<BadRequestObjectResult>());

        _controller.ModelState.AddModelError("Description", "Required");
        Assert.That(await _controller.CreatePeriod(missingDates), Is.InstanceOf<BadRequestObjectResult>());
        Assert.That(await _controller.UpdatePeriod(1, missingDates), Is.InstanceOf<BadRequestObjectResult>());
        _controller.ModelState.Clear();

        var valid = new PeriodCreationModel { Description = "2026-2027", From = DateTime.Today, Until = DateTime.Today.AddDays(5) };
        _periodServiceMock.Setup(s => s.UpdatePeriodAsync(99, valid.Description, valid.From.Value, valid.Until.Value))
            .ThrowsAsync(new DataNotFoundException());
        Assert.That(await _controller.UpdatePeriod(99, valid), Is.InstanceOf<NotFoundResult>());

        _periodServiceMock.Setup(s => s.UpdatePeriodAsync(98, valid.Description, valid.From.Value, valid.Until.Value))
            .ThrowsAsync(new ContractException("Bad range"));
        Assert.That(await _controller.UpdatePeriod(98, valid), Is.InstanceOf<BadRequestObjectResult>());

        _periodServiceMock.Setup(s => s.DeletePeriodAsync(99)).ThrowsAsync(new DataNotFoundException());
        Assert.That(await _controller.DeletePeriod(99), Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public void NullableDateTimeJsonConverter_ShouldReadAndWriteNullAndDateValues()
    {
        const string json = """{"description":"2026-2027","from":null,"until":"2027-06-30T00:00:00Z"}""";
        var model = JsonSerializer.Deserialize<PeriodCreationModel>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        Assert.That(model.From, Is.Null);
        Assert.That(model.Until, Is.Not.Null);

        string serialized = JsonSerializer.Serialize(model);
        Assert.That(serialized, Does.Contain("\"From\":null"));
        Assert.That(serialized, Does.Contain("2027-06-30"));
    }
}
