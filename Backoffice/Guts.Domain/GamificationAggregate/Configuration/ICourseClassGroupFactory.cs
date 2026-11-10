namespace Guts.Domain.GamificationAggregate.Configuration;

public interface ICourseClassGroupFactory
{
    CourseClassGroup CreateNew(string name, int courseId, int periodId);
}