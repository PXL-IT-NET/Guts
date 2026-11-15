using System.Collections.Generic;

namespace Guts.Domain.CourseAggregate;

public interface ICourseFactory
{
    Course CreateNew(string code, string name, IReadOnlyCollection<Course> existingCourses);
}
