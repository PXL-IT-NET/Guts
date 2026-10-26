namespace Guts.Domain.GamificationAggregate;

public interface ICourseClassGroupFactory
{
    CourseClassGroup CreateNew(string name, int courseId, int periodId);
}