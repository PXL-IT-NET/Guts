namespace Guts.Domain.GamificationAggregate.Profiles;

public interface IStudentCourseProfileFactory
{
    StudentCourseProfile CreateNew(int userId, int courseId, int periodId);
}