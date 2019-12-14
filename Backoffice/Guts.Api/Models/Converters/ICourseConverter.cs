using System.Collections.Generic;
using Guts.Domain.CourseAggregate;
using Guts.Domain.TopicAggregate.ChapterAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;

namespace Guts.Api.Models.Converters
{
    public interface ICourseConverter
    {
        CourseContentsModel ToCourseContentsModel(Course course, IList<Chapter> chapters, IList<Project> projects);
    }
}