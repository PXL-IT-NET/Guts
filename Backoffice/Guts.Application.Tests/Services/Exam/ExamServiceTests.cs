using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Guts.Application.Converters;
using Guts.Application.Dtos.Exam;
using Guts.Application.Repositories;
using Guts.Application.Security;
using Guts.Application.Services.Exam;
using Guts.Common;
using Guts.Domain.ExamAggregate;
using Guts.Domain.PeriodAggregate;
using Guts.Domain.Tests.Builders;
using Moq;
using NUnit.Framework;

namespace Guts.Application.Tests.Services.Exam;

[TestFixture]
public class ExamServiceTests
{
    private Mock<IExamRepository> _examRepositoryMock;
    private Mock<IExamFactory> _examFactoryMock;
    private Mock<IPeriodRepository> _periodRepositoryMock;
    private Mock<IExamProjectImporter> _projectImporterMock;
    private Mock<IExamConverter> _examConverterMock;
    private Mock<ITokenAccessPassFactory> _tokenFactoryMock;
    private ExamService _service;

    [SetUp]
    public void Setup()
    {
        _examRepositoryMock = new Mock<IExamRepository>();
        _examFactoryMock = new Mock<IExamFactory>();
        _periodRepositoryMock = new Mock<IPeriodRepository>();
        _projectImporterMock = new Mock<IExamProjectImporter>();
        _examConverterMock = new Mock<IExamConverter>();
        _tokenFactoryMock = new Mock<ITokenAccessPassFactory>();

        _service = new ExamService(
            _examRepositoryMock.Object,
            _examFactoryMock.Object,
            _periodRepositoryMock.Object,
            _projectImporterMock.Object,
            _examConverterMock.Object,
            _tokenFactoryMock.Object);
    }

    [Test]
    public async Task GetCourseExamsAsync_ResolvesPeriodAndReturnsSummaries()
    {
        var period = new PeriodBuilder().WithId().Build();
        var exam = new ExamBuilder().WithId(1).Build();
        _periodRepositoryMock.Setup(r => r.GetPeriodAsync(null)).ReturnsAsync(period);
        _examRepositoryMock.Setup(r => r.GetByCourseAndPeriodAsync(10, period.Id)).ReturnsAsync([exam]);
        _examConverterMock.Setup(c => c.ToSummaryDto(exam)).Returns(new ExamSummaryDto { Id = 1 });

        var result = await _service.GetCourseExamsAsync(10);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Id, Is.EqualTo(1));
    }

    [Test]
    public async Task CreateExamAsync_ResolvesPeriodCreatesViaFactoryAndPersists()
    {
        var period = new PeriodBuilder().WithId().Build();
        var createdExam = new ExamBuilder().WithId(12).Build();
        _periodRepositoryMock.Setup(r => r.GetPeriodAsync(null)).ReturnsAsync(period);
        _examFactoryMock.Setup(f => f.CreateNew(10, period.Id, 7, It.IsAny<ExamMetadata>(), It.IsAny<DateTime>()))
            .Returns(createdExam);
        _examRepositoryMock.Setup(r => r.AddAsync(createdExam)).ReturnsAsync(createdExam);
        _examConverterMock.Setup(c => c.ToDetailDto(createdExam)).Returns(new ExamDetailDto { Id = 12 });

        var result = await _service.CreateExamAsync(new CreateExamRequestDto
        {
            CourseId = 10,
            CreatedByUserId = 7,
            Title = "Midterm",
            Description = "Desc",
            AccessCode = "CODE-1",
            TemplateKey = ExamTemplateCatalog.DotNet10Console
        });

        Assert.That(result.Id, Is.EqualTo(12));
        _examRepositoryMock.Verify(r => r.AddAsync(createdExam), Times.Once);
    }

    [Test]
    public async Task OpenAndCloseExamAsync_InvokeAggregateTransitionsAndPersist()
    {
        var exam = new ExamBuilder().WithId(1).WithQuestion().Build();
        _examRepositoryMock.Setup(r => r.GetByIdWithQuestionsAsync(1)).ReturnsAsync(exam);
        _examConverterMock.Setup(c => c.ToDetailDto(exam)).Returns(() => new ExamDetailDto { Status = exam.Status });

        var opened = await _service.OpenExamAsync(1);
        Assert.That(opened.Status, Is.EqualTo(ExamStatus.Open));

        var closed = await _service.CloseExamAsync(1);
        Assert.That(closed.Status, Is.EqualTo(ExamStatus.Closed));
        _examRepositoryMock.Verify(r => r.UpdateAsync(exam), Times.Exactly(2));
    }

    [Test]
    public async Task ImportQuestionFilesFromZipAsync_ReplacesFilesAndPersists()
    {
        var exam = new ExamBuilder().WithId(1).WithQuestion().Build();
        var questionId = exam.Questions.First().Id;
        _examRepositoryMock.Setup(r => r.GetByIdWithQuestionsAsync(1)).ReturnsAsync(exam);
        _projectImporterMock.Setup(i => i.ExtractFromZipAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ImportedProjectFileDto { RelativePath = "Program.cs", Content = "class P {}", SuggestedRole = ExamFileRole.Editable },
                new ImportedProjectFileDto { RelativePath = "Tests.cs", Content = "class T {}", SuggestedRole = ExamFileRole.PublicTest }
            ]);
        _examConverterMock.Setup(c => c.ToDetailDto(exam)).Returns(new ExamDetailDto { Id = 1 });

        using var ms = new MemoryStream([1, 2, 3]);
        await _service.ImportQuestionFilesFromZipAsync(1, questionId, ms);

        Assert.That(exam.Questions.First().Files, Has.Count.EqualTo(2));
        _examRepositoryMock.Verify(r => r.UpdateAsync(exam), Times.Once);
    }

    [Test]
    public async Task ImportQuestionFilesFromGitAsync_And_UpdateQuestionFilesAsync_WorkCorrectly()
    {
        var exam = new ExamBuilder().WithId(1).WithQuestion().Build();
        var question = exam.Questions.First();
        _examRepositoryMock.Setup(r => r.GetByIdWithQuestionsAsync(1)).ReturnsAsync(exam);
        _projectImporterMock.Setup(i => i.CloneFromGitAsync("https://github.com/org/repo.git", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ImportedProjectFileDto { RelativePath = "App.cs", Content = "class App {}", SuggestedRole = ExamFileRole.Editable }
            ]);
        _examConverterMock.Setup(c => c.ToDetailDto(exam)).Returns(new ExamDetailDto { Id = 1 });

        await _service.ImportQuestionFilesFromGitAsync(1, question.Id, "https://github.com/org/repo.git", "main");
        Assert.That(question.Files.Single().RelativePath.Value, Is.EqualTo("App.cs"));

        await _service.UpdateQuestionFilesAsync(1, question.Id,
        [
            new FileRoleAssignmentDto { FileId = 0, Role = ExamFileRole.ReadOnly },
            new FileRoleAssignmentDto { RelativePath = "NewFile.cs", Content = "class New {}", Role = ExamFileRole.Editable }
        ]);
        Assert.That(question.Files, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task EnterExamAsync_ValidCodeAndOpen_ReturnsGuestTokenAndWorkspace()
    {
        var exam = new ExamBuilder().WithId(42).WithQuestion().Build();
        exam.Open(DateTime.UtcNow);
        var pass = new TokenAccessPass { Token = "guest-jwt", Expiration = DateTime.UtcNow.AddHours(8) };
        var workspace = new ExamWorkspaceDto { ExamId = 42 };

        _examRepositoryMock.Setup(r => r.GetByIdWithQuestionsAsync(42)).ReturnsAsync(exam);
        _tokenFactoryMock.Setup(t => t.CreateExamGuestToken(42, 480)).Returns(pass);
        _examConverterMock.Setup(c => c.ToWorkspaceDto(exam)).Returns(workspace);

        var result = await _service.EnterExamAsync(42, "EXAM-2026");

        Assert.That(result.AccessPass, Is.SameAs(pass));
        Assert.That(result.Workspace, Is.SameAs(workspace));
    }

    [Test]
    public void GetExamWorkspaceAsync_WhenNotOpenAndPreviewFalse_ThrowsContractException()
    {
        var exam = new ExamBuilder().WithId(42).WithQuestion().Build();
        _examRepositoryMock.Setup(r => r.GetByIdWithQuestionsAsync(42)).ReturnsAsync(exam);

        Assert.ThrowsAsync<ContractException>(() => _service.GetExamWorkspaceAsync(42, allowUnopenedPreview: false));
    }

    [Test]
    public async Task GetExamWorkspaceAsync_WhenPreviewTrueOnDraft_ReturnsWorkspace()
    {
        var exam = new ExamBuilder().WithId(42).WithQuestion().Build();
        var workspace = new ExamWorkspaceDto { ExamId = 42 };
        _examRepositoryMock.Setup(r => r.GetByIdWithQuestionsAsync(42)).ReturnsAsync(exam);
        _examConverterMock.Setup(c => c.ToWorkspaceDto(exam)).Returns(workspace);

        var result = await _service.GetExamWorkspaceAsync(42, allowUnopenedPreview: true);

        Assert.That(result, Is.SameAs(workspace));
    }

    [Test]
    public async Task LectorCrudAndQuestionManagementMethods_OrchestrateRepositoryAndConverter()
    {
        var exam = new ExamBuilder().WithId(1).WithQuestion().Build();
        _examRepositoryMock.Setup(r => r.GetByIdWithQuestionsAsync(1)).ReturnsAsync(exam);
        _examConverterMock.Setup(c => c.ToDetailDto(exam)).Returns(() => new ExamDetailDto { Id = exam.Id, Title = exam.Title });

        var detail = await _service.GetExamForLectorAsync(1);
        Assert.That(detail.Id, Is.EqualTo(1));

        var updated = await _service.UpdateExamAsync(1, new UpdateExamRequestDto
        {
            Title = "Updated Title",
            Description = "Updated",
            AccessCode = "NEW-99",
            TemplateKey = ExamTemplateCatalog.DotNet10Console
        });
        Assert.That(updated.Title, Is.EqualTo("Updated Title"));

        await _service.AddQuestionAsync(1, new SaveExamQuestionDto
        {
            Title = "Q2",
            InstructionsMarkdown = "Do Q2",
            OrderIndex = 2,
            TemplateKey = ExamTemplateCatalog.DotNet10Console
        });
        Assert.That(exam.Questions, Has.Count.EqualTo(2));

        await _service.UpdateQuestionAsync(1, 0, new SaveExamQuestionDto
        {
            Title = "Q1 Edited",
            InstructionsMarkdown = "Edited",
            OrderIndex = 3,
            TemplateKey = ExamTemplateCatalog.DotNet10ClassLibXUnit
        });
        Assert.That(exam.Questions.First().Title, Is.EqualTo("Q1 Edited"));

        await _service.DeleteQuestionFileAsync(1, 0, 0);
        Assert.That(exam.Questions.First().Files, Is.Empty);

        await _service.DeleteQuestionAsync(1, 0);
        Assert.That(exam.Questions, Has.Count.EqualTo(1));

        await _service.DeleteExamAsync(1);
        _examRepositoryMock.Verify(r => r.DeleteAsync(exam), Times.Once);
    }
}
