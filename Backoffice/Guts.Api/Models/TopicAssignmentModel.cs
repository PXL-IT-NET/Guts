using AutoMapper;
using Guts.Api.Models.AssignmentModels;
using Guts.Domain.AssignmentAggregate;

namespace Guts.Api.Models
{
    public class TopicAssignmentModel : AssignmentModel
    {
        public string TopicCode { get; set; }

        public string TopicDescription { get; set; }

        public int NumberOfTests { get; set; }

        private class MappingProfile : Profile
        {
            public MappingProfile()
            {
                CreateMap<Assignment, TopicAssignmentModel>()
                    .ForMember(dest => dest.AssignmentId, opt => opt.MapFrom(src => src.Id))
                    .ForMember(dest => dest.TopicCode, opt => opt.MapFrom(src => src.Topic.Code))
                    .ForMember(dest => dest.TopicDescription, opt => opt.MapFrom(src => src.Topic.Description))
                    .ForMember(dest => dest.NumberOfTests, opt => opt.MapFrom(src => src.Tests.Count));
            }
        }
    }
}