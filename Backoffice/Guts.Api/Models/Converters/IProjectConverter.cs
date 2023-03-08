using Guts.Domain.TopicAggregate.ProjectAggregate;

namespace Guts.Api.Models.Converters
{
    public interface IProjectConverter
    {
        TopicModel ToTopicModel(IProject project);
        ProjectDetailModel ToProjectDetailModel(IProject project);
    }
}