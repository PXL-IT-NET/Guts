using System.Linq;
using Guts.Api.Models.AssignmentModels;
using Guts.Api.Models.ProjectModels;
using Guts.Domain.TopicAggregate.ProjectAggregate;

namespace Guts.Api.Models.Converters
{
    public class ProjectConverter : IProjectConverter
    {
        public TopicModel ToTopicModel(IProject project)
        {
            return new TopicModel
            {
                Id = project.Id,
                Code = project.Code,
                Description = project.Description
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
                }).ToList(),
                Components = project.Assignments.Select(a => new AssignmentModel
                {
                    AssignmentId = a.Id,
                    Code = a.Code
                }).ToList()
            };

            return model;
        }
    }
}