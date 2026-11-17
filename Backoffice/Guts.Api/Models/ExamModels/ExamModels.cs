using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Guts.Domain.ExamAggregate;

namespace Guts.Api.Models.ExamModels;

public class ExamTemplateOptionModel
{
    public string Key { get; set; }
    public string DisplayName { get; set; }
    public bool SupportsWasmExecution { get; set; }
}

public class ExamSummaryModel
{
    public int Id { get; set; }
    public string Title { get; set; }
    public int CourseId { get; set; }
    public int PeriodId { get; set; }
    public string TemplateKey { get; set; }
    public string Status { get; set; }
    public int QuestionCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class ExamDetailModel : ExamSummaryModel
{
    public string Description { get; set; }
    public string AccessCode { get; set; }
    public int CreatedByUserId { get; set; }
    public IReadOnlyList<ExamQuestionModel> Questions { get; set; } = [];
}

public class ExamQuestionModel
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string InstructionsMarkdown { get; set; }
    public int OrderIndex { get; set; }
    public string TemplateKey { get; set; }
    public IReadOnlyList<ExamQuestionFileModel> Files { get; set; } = [];
}

public class ExamQuestionFileModel
{
    public int Id { get; set; }
    public string RelativePath { get; set; }
    public string Content { get; set; }
    public ExamFileRole Role { get; set; }
}

public class ExamWorkspaceModel
{
    public int ExamId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string TemplateKey { get; set; }
    public string Status { get; set; }
    public IReadOnlyList<ExamQuestionModel> Questions { get; set; } = [];
}

public class CreateExamModel
{
    [Range(1, int.MaxValue)]
    public int CourseId { get; set; }
    public int? PeriodId { get; set; }
    [Required]
    public string Title { get; set; }
    public string Description { get; set; }
    [Required]
    public string AccessCode { get; set; }
    [Required]
    public string TemplateKey { get; set; }
}

public class UpdateExamModel
{
    [Required]
    public string Title { get; set; }
    public string Description { get; set; }
    [Required]
    public string AccessCode { get; set; }
    [Required]
    public string TemplateKey { get; set; }
}

public class SaveExamQuestionModel
{
    [Required]
    public string Title { get; set; }
    public string InstructionsMarkdown { get; set; }
    [Range(1, int.MaxValue)]
    public int OrderIndex { get; set; }
    [Required]
    public string TemplateKey { get; set; }
}

public class ImportGitModel
{
    [Required]
    public string RepositoryUrl { get; set; }
    public string BranchOrRef { get; set; }
}

public class UpdateExamFilesModel
{
    [Required]
    public IList<FileRoleAssignmentModel> Files { get; set; } = [];
}

public class FileRoleAssignmentModel
{
    public int? FileId { get; set; }
    public string RelativePath { get; set; }
    public string Content { get; set; }
    public ExamFileRole Role { get; set; }
}

public class EnterExamModel
{
    [Required]
    public string AccessCode { get; set; }
}

public class ExamEntryResponseModel
{
    public string Token { get; set; }
    public DateTime Expiration { get; set; }
    public ExamWorkspaceModel Workspace { get; set; }
}
