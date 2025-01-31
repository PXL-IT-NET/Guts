using AutoMapper;
using Guts.Domain.AssignmentAggregate;
using System.Collections.Generic;

namespace Guts.Api.Models.AssignmentModels
{
    public class AssignmentModel
    {
        public int AssignmentId { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public IList<TestModel> Tests { get; set; }

        private class MappingProfile : Profile
        {
            public MappingProfile()
            {
                CreateMap<Assignment, AssignmentModel>()
                    .ForMember(dest => dest.AssignmentId, opt => opt.MapFrom(src => src.Id));
            }
        }
    }
}