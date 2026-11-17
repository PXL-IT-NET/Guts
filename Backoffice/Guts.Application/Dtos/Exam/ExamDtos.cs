using System;
using System.Collections.Generic;
using Guts.Application.Security;
using Guts.Domain.ExamAggregate;

namespace Guts.Application.Dtos.Exam;

public class ExamSummaryDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public int CourseId { get; set; }
    public int PeriodId { get; set; }
    public string TemplateKey { get; set; }
    public ExamStatus Status { get; set; }
    public int QuestionCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class ExamDetailDto : ExamSummaryDto
{
    public string Description { get; set; }
    public string AccessCode { get; set; }
    public int CreatedByUserId { get; set; }
    public IReadOnlyList<ExamQuestionDto> Questions { get; set; } = [];
}

public class ExamQuestionDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string InstructionsMarkdown { get; set; }
    public int OrderIndex { get; set; }
    public string TemplateKey { get; set; }
    public IReadOnlyList<ExamQuestionFileDto> Files { get; set; } = [];
}

public class ExamQuestionFileDto
{
    public int Id { get; set; }
    public string RelativePath { get; set; }
    public string Content { get; set; }
    public ExamFileRole Role { get; set; }
}

public class ExamWorkspaceDto
{
    public int ExamId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string TemplateKey { get; set; }
    public ExamStatus Status { get; set; }
    public IReadOnlyList<ExamQuestionDto> Questions { get; set; } = [];
}

public class ExamEntryResultDto
{
    public TokenAccessPass AccessPass { get; set; }
    public ExamWorkspaceDto Workspace { get; set; }
}

public class ImportedProjectFileDto
{
    public string RelativePath { get; set; }
    public string Content { get; set; }
    public ExamFileRole SuggestedRole { get; set; }
}

public class FileRoleAssignmentDto
{
    public int? FileId { get; set; }
    public string RelativePath { get; set; }
    public string Content { get; set; }
    public ExamFileRole Role { get; set; }
}

public class CreateExamRequestDto
{
    public int CourseId { get; set; }
    public int? PeriodId { get; set; }
    public int CreatedByUserId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string AccessCode { get; set; }
    public string TemplateKey { get; set; }
}

public class UpdateExamRequestDto
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string AccessCode { get; set; }
    public string TemplateKey { get; set; }
}

public class SaveExamQuestionDto
{
    public string Title { get; set; }
    public string InstructionsMarkdown { get; set; }
    public int OrderIndex { get; set; }
    public string TemplateKey { get; set; }
}
