using System.Collections.Generic;
using AutoMapper;
using Guts.Domain.ProjectTeamAssessmentAggregate;
using Guts.Domain.ValueObjects;

namespace Guts.Api.Models.ProjectModels;

public class AssessmentResultModel
{
    public UserModel Subject { get; set; }

    public PeerAssessmentModel SelfAssessment { get; set; }

    public IList<PeerAssessmentModel> PeerAssessments { get; set; }

    public AssessmentSubResultModel AverageResult { get; set; }

    public AssessmentSubResultModel EffortResult { get; set; }
    public AssessmentSubResultModel CooperationResult { get; set; }
    public AssessmentSubResultModel ContributionResult { get; set; }

    private class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<IAssessmentResult, AssessmentResultModel>();
        }
    }
}