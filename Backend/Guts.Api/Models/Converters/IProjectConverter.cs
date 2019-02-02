using Guts.Domain;

namespace Guts.Api.Models.Converters
{
    public interface IProjectConverter
    {
        TopicModel ToProjectModel(Project project);
        ProjectDetailModel ToProjectDetailModel(Project project);
    }
}