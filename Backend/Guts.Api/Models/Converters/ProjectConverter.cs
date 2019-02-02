using System.Linq;
using Guts.Domain;

namespace Guts.Api.Models.Converters
{
    public class ProjectConverter : IProjectConverter
    {
        public TopicModel ToTopicModel(Project project)
        {
            return new TopicModel
            {
                Id = project.Id,
                Code = project.Code,
                Description = project.Description
            };
        }

        public ProjectDetailModel ToProjectDetailModel(Project project)
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