using AutoMapper;
using Guts.Business.Dtos;
using Guts.Domain.ProjectTeamAssessmentAggregate;

namespace Guts.Api.Models.ProjectModels;

public class PeerAssessmentModel
{
    public UserModel Subject { get; set; }
    public UserModel User { get; set; }

    public int ContributionScore { get; set; }
    public int CooperationScore { get; set; }
    public int EffortScore { get; set; }

    public bool IsSelfAssessment { get; set; }

    public string Explanation { get; set; }

    private class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<IPeerAssessment, PeerAssessmentModel>();
            CreateMap<PeerAssessmentModel, PeerAssessmentDto>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.User.Id))
                .ForMember(dest => dest.SubjectId, opt => opt.MapFrom(src => src.Subject.Id));
        }
    }
}