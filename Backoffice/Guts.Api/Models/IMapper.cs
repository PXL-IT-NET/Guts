using Guts.Api.Models.AssignmentModels;
using Guts.Api.Models.ExamModels;
using Guts.Api.Models.PeriodModels;
using Guts.Api.Models.ProjectModels;
using Guts.Application.Dtos;
using Guts.Application.Dtos.Exam;
using Guts.Domain.AssessmentResultAggregate;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.PeriodAggregate;
using Guts.Domain.ProjectTeamAssessmentAggregate;
using Guts.Domain.TestAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Guts.Domain.UserAggregate;

namespace Guts.Api.Models;

public interface IMapper
{
    UserModel MapToUserModel(User source);
    AssignmentModel MapToAssignmentModel(Assignment source);
    TopicAssignmentModel MapToTopicAssignmentModel(Assignment source);
    TestModel MapToTestModel(Test source);
    PeriodOutputModel MapToPeriodOutputModel(IPeriod source);
    ProjectAssessmentModel MapToProjectAssessmentModel(IProjectAssessment source);
    PeerAssessmentModel MapToPeerAssessmentModel(IPeerAssessment source);
    PeerAssessmentDto MapToPeerAssessmentDto(PeerAssessmentModel source);
    AssessmentSubResultModel MapToAssessmentSubResultModel(IAssessmentSubResult source);
    AssessmentResultModel MapToAssessmentResultModel(IAssessmentResult source);
    DeletionJobStatusModel MapToDeletionJobStatusModel(DeletionJobDto source);
    ExamSummaryModel MapToExamSummaryModel(ExamSummaryDto source);
    ExamDetailModel MapToExamDetailModel(ExamDetailDto source);
    ExamWorkspaceModel MapToExamWorkspaceModel(ExamWorkspaceDto source);
    ExamEntryResponseModel MapToExamEntryResponseModel(ExamEntryResultDto source);
}
