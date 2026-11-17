using System;
using Guts.Api.Models;
using Guts.Application.Dtos.Exam;
using Guts.Application.Security;
using Guts.Domain.ExamAggregate;
using NUnit.Framework;

namespace Guts.Api.Tests.Models;

[TestFixture]
public class ExamMapperTests
{
    private Mapper _mapper;

    [SetUp]
    public void Setup()
    {
        _mapper = new Mapper();
    }

    [Test]
    public void MapExamDtos_WhenNull_ReturnsNull()
    {
        Assert.That(_mapper.MapToExamSummaryModel(null), Is.Null);
        Assert.That(_mapper.MapToExamDetailModel(null), Is.Null);
        Assert.That(_mapper.MapToExamWorkspaceModel(null), Is.Null);
        Assert.That(_mapper.MapToExamEntryResponseModel(null), Is.Null);
    }

    [Test]
    public void MapExamDtos_MapsAllFieldsCorrectly()
    {
        var now = DateTime.UtcNow;
        var summaryDto = new ExamSummaryDto
        {
            Id = 1,
            Title = "Midterm",
            CourseId = 2,
            PeriodId = 3,
            TemplateKey = ExamTemplateCatalog.DotNet10Console,
            Status = ExamStatus.Open,
            QuestionCount = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        var questionDto = new ExamQuestionDto
        {
            Id = 10,
            Title = "Q1",
            InstructionsMarkdown = "# Q1",
            OrderIndex = 1,
            TemplateKey = ExamTemplateCatalog.DotNet10Console,
            Files = [new ExamQuestionFileDto { Id = 100, RelativePath = "Program.cs", Content = "class P {}", Role = ExamFileRole.Editable }]
        };
        var detailDto = new ExamDetailDto
        {
            Id = 1,
            Title = "Midterm",
            Description = "Desc",
            AccessCode = "CODE",
            CreatedByUserId = 9,
            CourseId = 2,
            PeriodId = 3,
            TemplateKey = ExamTemplateCatalog.DotNet10Console,
            Status = ExamStatus.Open,
            QuestionCount = 1,
            Questions = [questionDto]
        };
        var workspaceDto = new ExamWorkspaceDto
        {
            ExamId = 1,
            Title = "Midterm",
            Description = "Desc",
            TemplateKey = ExamTemplateCatalog.DotNet10Console,
            Status = ExamStatus.Open,
            Questions = [questionDto]
        };
        var entryDto = new ExamEntryResultDto
        {
            AccessPass = new TokenAccessPass { Token = "tok", Expiration = now.AddHours(1) },
            Workspace = workspaceDto
        };

        var summary = _mapper.MapToExamSummaryModel(summaryDto);
        Assert.That(summary.Status, Is.EqualTo("Open"));

        var detail = _mapper.MapToExamDetailModel(detailDto);
        Assert.That(detail.AccessCode, Is.EqualTo("CODE"));
        Assert.That(detail.Questions[0].Files[0].RelativePath, Is.EqualTo("Program.cs"));

        var entry = _mapper.MapToExamEntryResponseModel(entryDto);
        Assert.That(entry.Token, Is.EqualTo("tok"));
        Assert.That(entry.Workspace.ExamId, Is.EqualTo(1));
    }
}
