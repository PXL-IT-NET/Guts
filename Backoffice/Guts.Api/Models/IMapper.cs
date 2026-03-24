using Guts.Api.Models.AssignmentModels;
using Guts.Api.Models.ExamModels;
using Guts.Api.Models.PeriodModels;
using Guts.Api.Models.ProjectModels;
using Guts.Business.Dtos;
using Guts.Domain.AssessmentResultAggregate;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.ExamAggregate;
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
    ExamOutputModel MapToExamOutputModel(IExam source);
    ExamPartOutputModel MapToExamPartOutputModel(IExamPart source);
    AssignmentEvaluationOutputModel MapToAssignmentEvaluationOutputModel(IAssignmentEvaluation source);
    PeriodOutputModel MapToPeriodOutputModel(IPeriod source);
    ProjectAssessmentModel MapToProjectAssessmentModel(IProjectAssessment source);
    PeerAssessmentModel MapToPeerAssessmentModel(IPeerAssessment source);
    PeerAssessmentDto MapToPeerAssessmentDto(PeerAssessmentModel source);
    AssessmentSubResultModel MapToAssessmentSubResultModel(IAssessmentSubResult source);
    AssessmentResultModel MapToAssessmentResultModel(IAssessmentResult source);
}
