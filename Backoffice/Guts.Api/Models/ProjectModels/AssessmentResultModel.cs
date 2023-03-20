using System.Collections.Generic;
using AutoMapper;
using Guts.Domain.ProjectTeamAssessmentAggregate;

namespace Guts.Api.Models.ProjectModels;

public class AssessmentResultModel
{
    public UserModel Subject { get; set; }

    public PeerAssessmentModel SelfAssessment { get; set; }

    public IList<PeerAssessmentModel> PeerAssessments { get; set; }

    public IAssessmentSubResult AverageResult { get; set; }

    public IAssessmentSubResult EffortResult { get; set; }
    public IAssessmentSubResult CooperationResult { get; set; }
    public IAssessmentSubResult ContributionResult { get; set; }

    private class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<IAssessmentResult, AssessmentResultModel>();
        }
    }
}