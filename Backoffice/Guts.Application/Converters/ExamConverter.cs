using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Application.Dtos.Exam;
using Guts.Domain.ExamAggregate;
using ExamEntity = Guts.Domain.ExamAggregate.Exam;

namespace Guts.Application.Converters;

public class ExamConverter : IExamConverter
{
    public ExamSummaryDto ToSummaryDto(ExamEntity exam)
    {
        ArgumentNullException.ThrowIfNull(exam);

        return new ExamSummaryDto
        {
            Id = exam.Id,
            Title = exam.Title,
            CourseId = exam.CourseId,
            PeriodId = exam.PeriodId,
            TemplateKey = exam.TemplateKey,
            Status = exam.Status,
            QuestionCount = exam.Questions.Count,
            CreatedAtUtc = exam.CreatedAtUtc,
            UpdatedAtUtc = exam.UpdatedAtUtc
        };
    }

    public ExamDetailDto ToDetailDto(ExamEntity exam)
    {
        ArgumentNullException.ThrowIfNull(exam);

        return new ExamDetailDto
        {
            Id = exam.Id,
            Title = exam.Title,
            Description = exam.Description,
            CourseId = exam.CourseId,
            PeriodId = exam.PeriodId,
            AccessCode = exam.AccessCode?.Value,
            TemplateKey = exam.TemplateKey,
            Status = exam.Status,
            CreatedByUserId = exam.CreatedByUserId,
            QuestionCount = exam.Questions.Count,
            CreatedAtUtc = exam.CreatedAtUtc,
            UpdatedAtUtc = exam.UpdatedAtUtc,
            Questions = MapQuestions(exam.Questions)
        };
    }

    public ExamWorkspaceDto ToWorkspaceDto(ExamEntity exam)
    {
        ArgumentNullException.ThrowIfNull(exam);

        return new ExamWorkspaceDto
        {
            ExamId = exam.Id,
            Title = exam.Title,
            Description = exam.Description,
            TemplateKey = exam.TemplateKey,
            Status = exam.Status,
            Questions = MapQuestions(exam.Questions)
        };
    }

    private static IReadOnlyList<ExamQuestionDto> MapQuestions(IEnumerable<ExamQuestion> questions)
    {
        return questions
            .OrderBy(q => q.OrderIndex)
            .Select(q => new ExamQuestionDto
            {
                Id = q.Id,
                Title = q.Title,
                InstructionsMarkdown = q.InstructionsMarkdown,
                OrderIndex = q.OrderIndex,
                TemplateKey = q.TemplateKey,
                Files = q.Files
                    .OrderBy(f => f.RelativePath.Value, StringComparer.OrdinalIgnoreCase)
                    .Select(f => new ExamQuestionFileDto
                    {
                        Id = f.Id,
                        RelativePath = f.RelativePath.Value,
                        Content = f.Content,
                        Role = f.Role
                    })
                    .ToList()
            })
            .ToList();
    }
}
