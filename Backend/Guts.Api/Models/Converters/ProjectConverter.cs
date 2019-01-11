using Guts.Domain;

namespace Guts.Api.Models.Converters
{
    public class ProjectConverter : IProjectConverter
    {
        public ProjectModel ToProjectModel(Project project)
        {
            return new ProjectModel
            {
                Id = project.Id,
                Code = project.Code,
                Description = project.Description
            };
        }
    }
}