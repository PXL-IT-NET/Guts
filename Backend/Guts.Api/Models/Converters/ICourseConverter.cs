using System.Collections.Generic;
using Guts.Domain;

namespace Guts.Api.Models.Converters
{
    public interface ICourseConverter
    {
        CourseContentsModel ToCourseContentsModel(Course course, IList<Chapter> chapters, IList<Project> projects);
    }
}