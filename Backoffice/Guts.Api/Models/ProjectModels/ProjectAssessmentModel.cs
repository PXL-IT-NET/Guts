using System;
using AutoMapper;
using Guts.Domain.TopicAggregate.ProjectAggregate;

namespace Guts.Api.Models.ProjectModels;

public class ProjectAssessmentModel
{
    public int Id { get; set; }
    public string Description { get; set; }

    public DateTime OpenOnUtc { get; set; }

    public DateTime DeadlineUtc { get; set; }

    private class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<IProjectAssessment, ProjectAssessmentModel>();
        }
    }
}