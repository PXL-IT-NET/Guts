namespace Guts.Domain.GamificationAggregate
{
    public interface IStudentCourseProfileFactory
    {
        StudentCourseProfile CreateNew(int userId, int courseId, int periodId);
    }
}
