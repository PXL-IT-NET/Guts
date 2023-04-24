using AutoMapper;
using Guts.Domain.UserAggregate;

namespace Guts.Api.Models
{
    public class UserModel
    {
        public int Id { get; set; }
        public string FullName { get; set; }

        private class MappingProfile : Profile
        {
            public MappingProfile()
            {
                CreateMap<User, UserModel>()
                    .ForMember(dest => dest.FullName,opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}".Trim()));
            }
        }
    }
}