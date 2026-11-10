#pragma warning disable RMG089 // Implicit user-defined conversion on AssessmentScore value object is intentional
using System.Linq;
using Guts.Api.Models.AssignmentModels;
using Guts.Api.Models.PeriodModels;
using Guts.Api.Models.ProjectModels;
using Guts.Application.Dtos;
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

    // ── Mapperly source-generated mappings ───────────────────────────────────

    [MapperIgnoreSource(nameof(Test.Assignment))]
    [MapperIgnoreSource(nameof(Test.AssignmentId))]
    [MapperIgnoreSource(nameof(Test.Results))]
    public partial TestModel MapToTestModel(Test source);

    public partial PeriodOutputModel MapToPeriodOutputModel(IPeriod source);

    [MapperIgnoreSource(nameof(IProjectAssessment.ProjectId))]
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
