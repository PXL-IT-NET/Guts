using AutoMapper;
using Guts.Domain.AssessmentResultAggregate;

namespace Guts.Api.Models.ProjectModels;

public class AssessmentSubResultModel
{
    public double Value { get; set; }
    public double SelfValue { get; set; }
    public double PeerValue { get; set; }

    public int Score { get; set; }
    public int SelfScore { get; set; }
    public int PeerScore { get; set; }

    public double AverageValue { get; set; }
    public double AverageSelfValue { get; set; }
    public double AveragePeerValue { get; set; }

    private class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<IAssessmentSubResult, AssessmentSubResultModel>();
        }
    }
}