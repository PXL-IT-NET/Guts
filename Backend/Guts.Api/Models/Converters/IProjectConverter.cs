using Guts.Domain;

namespace Guts.Api.Models.Converters
{
    public interface IProjectConverter
    {
        ProjectModel ToProjectModel(Project project);
    }
}