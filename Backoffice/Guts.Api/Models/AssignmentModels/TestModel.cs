using AutoMapper;
using Guts.Domain.TestAggregate;

namespace Guts.Api.Models.AssignmentModels;

public class TestModel
{
    public int Id { get; set; }
    public string TestName { get; set; }

    private class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Test, TestModel>();
        }
    }
}