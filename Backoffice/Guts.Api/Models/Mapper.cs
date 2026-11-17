#pragma warning disable RMG089 // Implicit user-defined conversion on AssessmentScore value object is intentional
using System.Linq;
using Guts.Api.Models.AssignmentModels;
using Guts.Api.Models.ExamModels;
using Guts.Api.Models.PeriodModels;
using Guts.Api.Models.ProjectModels;
using Guts.Application.Dtos;
using Guts.Application.Dtos.Exam;
using Guts.Domain;
using Guts.Domain.AssessmentResultAggregate;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.PeriodAggregate;
using Guts.Domain.ProjectTeamAssessmentAggregate;
using Guts.Domain.TestAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Guts.Domain.UserAggregate;
using Riok.Mapperly.Abstractions;

namespace Guts.Api.Models;

[Mapper]
public partial class Mapper : IMapper
{
    // ── Manual mappings (custom logic) ──────────────────────────────────────

    public UserModel MapToUserModel(User source)
    {
        return new UserModel
        {
            Id = source.Id,
            FullName = source.FullName
        };
    }

    public AssignmentModel MapToAssignmentModel(Assignment source)
    {
        return new AssignmentModel
        {
            AssignmentId = source.Id,
            Code = source.Code,
            Description = source.Description,
            Tests = source.Tests?.Select(MapToTestModel).ToList()
        };
    }

    public TopicAssignmentModel MapToTopicAssignmentModel(Assignment source)
    {
        return new TopicAssignmentModel
        {
            AssignmentId = source.Id,
            Code = source.Code,
            Description = source.Description,
            Tests = source.Tests?.Select(MapToTestModel).ToList(),
            TopicCode = source.Topic?.Code,
            TopicDescription = source.Topic?.Description,
            NumberOfTests = source.Tests?.Count ?? 0
        };
    }

    public DeletionJobStatusModel MapToDeletionJobStatusModel(DeletionJobDto source)
    {
        if (source == null) return null;

        return new DeletionJobStatusModel
        {
            JobId = source.JobId,
            TargetType = source.TargetType.ToString(),
            TargetId = source.TargetId,
            TargetLabel = source.TargetLabel,
            Status = source.Status.ToString(),
            TotalRows = source.TotalRows,
            DeletedRows = source.DeletedRows,
            Percentage = source.Percentage,
            CurrentPhase = source.CurrentPhase,
            ErrorMessage = source.ErrorMessage,
            CreatedAtUtc = source.CreatedAtUtc,
            StartedAtUtc = source.StartedAtUtc,
            CompletedAtUtc = source.CompletedAtUtc
        };
    }

    public ExamSummaryModel MapToExamSummaryModel(ExamSummaryDto source)
    {
        if (source == null) return null;

        return new ExamSummaryModel
        {
            Id = source.Id,
            Title = source.Title,
            CourseId = source.CourseId,
            PeriodId = source.PeriodId,
            TemplateKey = source.TemplateKey,
            Status = source.Status.ToString(),
            QuestionCount = source.QuestionCount,
            CreatedAtUtc = source.CreatedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc
        };
    }

    public ExamDetailModel MapToExamDetailModel(ExamDetailDto source)
    {
        if (source == null) return null;

        return new ExamDetailModel
        {
            Id = source.Id,
            Title = source.Title,
            Description = source.Description,
            CourseId = source.CourseId,
            PeriodId = source.PeriodId,
            AccessCode = source.AccessCode,
            TemplateKey = source.TemplateKey,
            Status = source.Status.ToString(),
            CreatedByUserId = source.CreatedByUserId,
            QuestionCount = source.QuestionCount,
            CreatedAtUtc = source.CreatedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc,
            Questions = source.Questions.Select(MapToExamQuestionModel).ToList()
        };
    }

    public ExamWorkspaceModel MapToExamWorkspaceModel(ExamWorkspaceDto source)
    {
        if (source == null) return null;

        return new ExamWorkspaceModel
        {
            ExamId = source.ExamId,
            Title = source.Title,
            Description = source.Description,
            TemplateKey = source.TemplateKey,
            Status = source.Status.ToString(),
            Questions = source.Questions.Select(MapToExamQuestionModel).ToList()
        };
    }

    public ExamEntryResponseModel MapToExamEntryResponseModel(ExamEntryResultDto source)
    {
        if (source == null) return null;

        return new ExamEntryResponseModel
        {
            Token = source.AccessPass?.Token,
            Expiration = source.AccessPass?.Expiration ?? default,
            Workspace = MapToExamWorkspaceModel(source.Workspace)
        };
    }

    private static ExamQuestionModel MapToExamQuestionModel(ExamQuestionDto source)
    {
        return new ExamQuestionModel
        {
            Id = source.Id,
            Title = source.Title,
            InstructionsMarkdown = source.InstructionsMarkdown,
            OrderIndex = source.OrderIndex,
            TemplateKey = source.TemplateKey,
            Files = source.Files.Select(f => new ExamQuestionFileModel
            {
                Id = f.Id,
                RelativePath = f.RelativePath,
                Content = f.Content,
                Role = f.Role
            }).ToList()
        };
    }

    // ── Mapperly source-generated mappings ───────────────────────────────────

    [MapperIgnoreSource(nameof(Test.Assignment))]
    [MapperIgnoreSource(nameof(Test.AssignmentId))]
    [MapperIgnoreSource(nameof(Test.Results))]
    [MapperIgnoreSource(nameof(IEntity.IdentityValues))]
    public partial TestModel MapToTestModel(Test source);

    [MapperIgnoreSource(nameof(IEntity.IdentityValues))]
    [MapperIgnoreSource(nameof(IPeriod.IsDeleting))]
    public partial PeriodOutputModel MapToPeriodOutputModel(IPeriod source);

    [MapperIgnoreSource(nameof(IProjectAssessment.ProjectId))]
    [MapperIgnoreSource(nameof(IEntity.IdentityValues))]
    public partial ProjectAssessmentModel MapToProjectAssessmentModel(IProjectAssessment source);

    [MapperIgnoreSource(nameof(IPeerAssessment.ProjectTeamAssessmentId))]
    public partial PeerAssessmentModel MapToPeerAssessmentModel(IPeerAssessment source);

    [MapperIgnoreSource(nameof(PeerAssessmentModel.IsSelfAssessment))]
    [MapProperty([nameof(PeerAssessmentModel.User), nameof(UserModel.Id)], nameof(PeerAssessmentDto.UserId))]
    [MapProperty([nameof(PeerAssessmentModel.Subject), nameof(UserModel.Id)], nameof(PeerAssessmentDto.SubjectId))]
    public partial PeerAssessmentDto MapToPeerAssessmentDto(PeerAssessmentModel source);

    public partial AssessmentSubResultModel MapToAssessmentSubResultModel(IAssessmentSubResult source);

    public partial AssessmentResultModel MapToAssessmentResultModel(IAssessmentResult source);
}
