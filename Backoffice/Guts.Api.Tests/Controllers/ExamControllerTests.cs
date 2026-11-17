using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Guts.Api.Controllers;
using Guts.Api.Models;
using Guts.Api.Models.ExamModels;
using Guts.Application.Dtos.Exam;
using Guts.Application.Services.Exam;
using Guts.Domain.ExamAggregate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace Guts.Api.Tests.Controllers;

[TestFixture]
public class ExamControllerTests
{
    private Mock<IExamService> _examServiceMock;
    private Mock<IMapper> _mapperMock;
    private ExamController _controller;

    [SetUp]
    public void Setup()
    {
        _examServiceMock = new Mock<IExamService>();
        _mapperMock = new Mock<IMapper>();
        _controller = new ExamController(_examServiceMock.Object, _mapperMock.Object);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "77"),
            new(ClaimTypes.Role, "lector")
        };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
            }
        };
    }

    [Test]
    public void Controller_ShouldRequireLectorOrAdminPolicy()
    {
        var attr = typeof(ExamController).GetCustomAttribute<AuthorizeAttribute>();

        Assert.That(attr, Is.Not.Null);
        Assert.That(attr!.Policy, Is.EqualTo(ApiConstants.LectorOrAdminPolicy));
    }

    [Test]
    public void GetTemplates_ReturnsCuratedCatalog()
    {
        var result = _controller.GetTemplates() as OkObjectResult;

        Assert.That(result, Is.Not.Null);
        var models = result!.Value as IList<ExamTemplateOptionModel>;
        Assert.That(models, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task GetCourseExams_And_GetExam_ValidateIdsAndReturnMappedModels()
    {
        Assert.That(await _controller.GetCourseExams(0), Is.InstanceOf<BadRequestResult>());
        Assert.That(await _controller.GetExam(0), Is.InstanceOf<BadRequestResult>());

        var summaryDto = new ExamSummaryDto { Id = 1 };
        var summaryModel = new ExamSummaryModel { Id = 1 };
        _examServiceMock.Setup(s => s.GetCourseExamsAsync(5, 2)).ReturnsAsync([summaryDto]);
        _mapperMock.Setup(m => m.MapToExamSummaryModel(summaryDto)).Returns(summaryModel);

        var listResult = await _controller.GetCourseExams(5, 2) as OkObjectResult;
        Assert.That(listResult?.Value as IList<ExamSummaryModel>, Has.Count.EqualTo(1));

        var detailDto = new ExamDetailDto { Id = 1 };
        var detailModel = new ExamDetailModel { Id = 1 };
        _examServiceMock.Setup(s => s.GetExamForLectorAsync(1)).ReturnsAsync(detailDto);
        _mapperMock.Setup(m => m.MapToExamDetailModel(detailDto)).Returns(detailModel);

        var detailResult = await _controller.GetExam(1) as OkObjectResult;
        Assert.That(detailResult?.Value, Is.SameAs(detailModel));
    }

    [Test]
    public async Task CreateUpdateOpenCloseDeleteExam_OrchestrateService()
    {
        var dto = new ExamDetailDto { Id = 10 };
        var model = new ExamDetailModel { Id = 10 };
        _examServiceMock.Setup(s => s.CreateExamAsync(It.Is<CreateExamRequestDto>(r => r.CreatedByUserId == 77)))
            .ReturnsAsync(dto);
        _examServiceMock.Setup(s => s.UpdateExamAsync(10, It.IsAny<UpdateExamRequestDto>())).ReturnsAsync(dto);
        _examServiceMock.Setup(s => s.OpenExamAsync(10)).ReturnsAsync(dto);
        _examServiceMock.Setup(s => s.CloseExamAsync(10)).ReturnsAsync(dto);
        _mapperMock.Setup(m => m.MapToExamDetailModel(dto)).Returns(model);

        var created = await _controller.CreateExam(new CreateExamModel
        {
            CourseId = 3,
            Title = "Exam",
            AccessCode = "CODE1",
            TemplateKey = ExamTemplateCatalog.DotNet10Console
        }) as CreatedAtActionResult;
        Assert.That(created?.Value, Is.SameAs(model));

        Assert.That(await _controller.UpdateExam(0, new UpdateExamModel()), Is.InstanceOf<BadRequestResult>());
        var updated = await _controller.UpdateExam(10, new UpdateExamModel { Title = "E2", AccessCode = "CODE2", TemplateKey = ExamTemplateCatalog.DotNet10Console }) as OkObjectResult;
        Assert.That(updated?.Value, Is.SameAs(model));

        Assert.That(await _controller.OpenExam(0), Is.InstanceOf<BadRequestResult>());
        Assert.That((await _controller.OpenExam(10) as OkObjectResult)?.Value, Is.SameAs(model));

        Assert.That(await _controller.CloseExam(0), Is.InstanceOf<BadRequestResult>());
        Assert.That((await _controller.CloseExam(10) as OkObjectResult)?.Value, Is.SameAs(model));

        Assert.That(await _controller.DeleteExam(0), Is.InstanceOf<BadRequestResult>());
        Assert.That(await _controller.DeleteExam(10), Is.InstanceOf<NoContentResult>());
        _examServiceMock.Verify(s => s.DeleteExamAsync(10), Times.Once);
    }

    [Test]
    public async Task QuestionImportFilesAndPreview_OrchestrateService()
    {
        var dto = new ExamDetailDto { Id = 10 };
        var detailModel = new ExamDetailModel { Id = 10 };
        var workspaceDto = new ExamWorkspaceDto { ExamId = 10 };
        var workspaceModel = new ExamWorkspaceModel { ExamId = 10 };

        _examServiceMock.Setup(s => s.AddQuestionAsync(10, It.IsAny<SaveExamQuestionDto>())).ReturnsAsync(dto);
        _examServiceMock.Setup(s => s.UpdateQuestionAsync(10, 2, It.IsAny<SaveExamQuestionDto>())).ReturnsAsync(dto);
        _examServiceMock.Setup(s => s.DeleteQuestionAsync(10, 2)).ReturnsAsync(dto);
        _examServiceMock.Setup(s => s.ImportQuestionFilesFromZipAsync(10, 2, It.IsAny<Stream>(), It.IsAny<CancellationToken>())).ReturnsAsync(dto);
        _examServiceMock.Setup(s => s.ImportQuestionFilesFromGitAsync(10, 2, "https://github.com/org/repo.git", "main", It.IsAny<CancellationToken>())).ReturnsAsync(dto);
        _examServiceMock.Setup(s => s.UpdateQuestionFilesAsync(10, 2, It.IsAny<IReadOnlyList<FileRoleAssignmentDto>>())).ReturnsAsync(dto);
        _examServiceMock.Setup(s => s.DeleteQuestionFileAsync(10, 2, 5)).ReturnsAsync(dto);
        _examServiceMock.Setup(s => s.GetExamWorkspaceAsync(10, true)).ReturnsAsync(workspaceDto);
        _mapperMock.Setup(m => m.MapToExamDetailModel(dto)).Returns(detailModel);
        _mapperMock.Setup(m => m.MapToExamWorkspaceModel(workspaceDto)).Returns(workspaceModel);

        Assert.That((await _controller.AddQuestion(10, new SaveExamQuestionModel { Title = "Q1", OrderIndex = 1, TemplateKey = ExamTemplateCatalog.DotNet10Console }) as OkObjectResult)?.Value, Is.SameAs(detailModel));
        Assert.That((await _controller.UpdateQuestion(10, 2, new SaveExamQuestionModel { Title = "Q1", OrderIndex = 1, TemplateKey = ExamTemplateCatalog.DotNet10Console }) as OkObjectResult)?.Value, Is.SameAs(detailModel));
        Assert.That((await _controller.DeleteQuestion(10, 2) as OkObjectResult)?.Value, Is.SameAs(detailModel));

        Assert.That(await _controller.ImportFromZip(10, 2, null, CancellationToken.None), Is.InstanceOf<BadRequestObjectResult>());
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(3);
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream([1, 2, 3]));
        Assert.That((await _controller.ImportFromZip(10, 2, fileMock.Object, CancellationToken.None) as OkObjectResult)?.Value, Is.SameAs(detailModel));

        Assert.That((await _controller.ImportFromGit(10, 2, new ImportGitModel { RepositoryUrl = "https://github.com/org/repo.git", BranchOrRef = "main" }, CancellationToken.None) as OkObjectResult)?.Value, Is.SameAs(detailModel));
        Assert.That((await _controller.UpdateFiles(10, 2, new UpdateExamFilesModel { Files = [new FileRoleAssignmentModel { FileId = 5, Role = ExamFileRole.ReadOnly }] }) as OkObjectResult)?.Value, Is.SameAs(detailModel));
        Assert.That((await _controller.DeleteFile(10, 2, 5) as OkObjectResult)?.Value, Is.SameAs(detailModel));
        Assert.That((await _controller.PreviewExam(10) as OkObjectResult)?.Value, Is.SameAs(workspaceModel));
    }
}
