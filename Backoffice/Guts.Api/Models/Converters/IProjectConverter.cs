using Guts.Domain.TopicAggregate.ProjectAggregate;

namespace Guts.Api.Models.Converters
{
    public interface IProjectConverter
    {
        TopicModel ToTopicModel(Project project);
        ProjectDetailModel ToProjectDetailModel(Project project);
    }
}