using System;
using System.Reflection;
using System.Threading.Tasks;
using Guts.Api.Controllers;
using Guts.Api.Filters;
using Guts.Api.Models;
using Guts.Api.Models.ExamModels;
using Guts.Application.Dtos.Exam;
using Guts.Application.Security;
using Guts.Application.Services.Exam;
using Guts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace Guts.Api.Tests.Controllers;

[TestFixture]
public class ExamSessionControllerTests
{
    private Mock<IExamService> _examServiceMock;
    private Mock<IMapper> _mapperMock;
    private ExamSessionController _controller;

    [SetUp]
    public void Setup()
    {
        _examServiceMock = new Mock<IExamService>();
        _mapperMock = new Mock<IMapper>();
        _controller = new ExamSessionController(_examServiceMock.Object, _mapperMock.Object);
    }

    [Test]
    public void Actions_ShouldHaveExpectedAllowAnonymousAndAllowExamGuestAttributes()
    {
        var enterMethod = typeof(ExamSessionController).GetMethod(nameof(ExamSessionController.Enter));
        var workspaceMethod = typeof(ExamSessionController).GetMethod(nameof(ExamSessionController.GetWorkspace));

        Assert.That(enterMethod!.GetCustomAttribute<AllowAnonymousAttribute>(), Is.Not.Null);
        Assert.That(workspaceMethod!.GetCustomAttribute<AllowExamGuestAttribute>(), Is.Not.Null);
    }

    [Test]
    public async Task Enter_InvalidExamIdOrModelState_ReturnsBadRequest()
    {
        Assert.That(await _controller.Enter(0, new EnterExamModel { AccessCode = "CODE" }), Is.InstanceOf<BadRequestResult>());

        _controller.ModelState.AddModelError("AccessCode", "Required");
        Assert.That(await _controller.Enter(1, new EnterExamModel()), Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Enter_InvalidCodeOrClosedExam_Returns403Forbidden()
    {
        _examServiceMock.Setup(s => s.EnterExamAsync(42, "WRONG"))
            .ThrowsAsync(new ContractException("Invalid exam access code."));

        var result = await _controller.Enter(42, new EnterExamModel { AccessCode = "WRONG" }) as ObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
    }

    [Test]
    public async Task Enter_ValidAccessCode_ReturnsOkWithEntryResponseModel()
    {
        var entryDto = new ExamEntryResultDto
        {
            AccessPass = new TokenAccessPass { Token = "jwt", Expiration = DateTime.UtcNow.AddHours(8) },
            Workspace = new ExamWorkspaceDto { ExamId = 42 }
        };
        var responseModel = new ExamEntryResponseModel { Token = "jwt" };

        _examServiceMock.Setup(s => s.EnterExamAsync(42, "EXAM-2026")).ReturnsAsync(entryDto);
        _mapperMock.Setup(m => m.MapToExamEntryResponseModel(entryDto)).Returns(responseModel);

        var result = await _controller.Enter(42, new EnterExamModel { AccessCode = "EXAM-2026" }) as OkObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value, Is.SameAs(responseModel));
    }

    [Test]
    public async Task GetWorkspace_ValidExamId_ReturnsOkWithWorkspaceModel()
    {
        Assert.That(await _controller.GetWorkspace(0), Is.InstanceOf<BadRequestResult>());

        var dto = new ExamWorkspaceDto { ExamId = 42 };
        var model = new ExamWorkspaceModel { ExamId = 42 };
        _examServiceMock.Setup(s => s.GetExamWorkspaceAsync(42, false)).ReturnsAsync(dto);
        _mapperMock.Setup(m => m.MapToExamWorkspaceModel(dto)).Returns(model);

        var result = await _controller.GetWorkspace(42) as OkObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value, Is.SameAs(model));
    }
}
