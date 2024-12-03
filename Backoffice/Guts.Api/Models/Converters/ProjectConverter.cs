using System.Linq;
using AutoMapper;
using Guts.Api.Models.AssignmentModels;
using Guts.Api.Models.ProjectModels;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.TopicAggregate.ChapterAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;

namespace Guts.Api.Models.Converters
{
    public class ProjectConverter : IProjectConverter
    {
        private readonly IMapper _mapper;

        public ProjectConverter(IMapper mapper)
        {
            _mapper = mapper;
        }
        public TopicModel ToTopicModel(IProject project)
        {
            return new TopicModel
            {
                Id = project.Id,
                Code = project.Code,
                Description = project.Description,
                Assignments = project.Assignments.Select(a => _mapper.Map<AssignmentModel>(a)).OrderBy(m => m.Code)
                    .ToList()
            };
        }

        public ProjectDetailModel ToProjectDetailModel(IProject project)
        {
            var model = new ProjectDetailModel
            {
                Id = project.Id,
                Code = project.Code,
                Description = project.Description,
                Teams = project.Teams.Select(t => new TeamModel
                {
                    Id = t.Id,
                    Name = t.Name
                }).OrderBy(team => team.Name).ToList(),
                Components = project.Assignments.Select(a => new AssignmentModel
                {
                    AssignmentId = a.Id,
                    Code = a.Code,
                    Description = a.Description,
                    Tests = a.Tests.Select(t => new TestModel { Id = t.Id, TestName = t.TestName }).ToList()
                }).OrderBy(c => c.Code).ToList()
            };

            return model;
        }
    }
}