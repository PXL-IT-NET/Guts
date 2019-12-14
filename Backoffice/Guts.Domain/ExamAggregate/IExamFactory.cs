namespace Guts.Domain.ExamAggregate
{
    public interface IExamFactory
    {
        Exam CreateNew(int courseId, int periodId, string name);
    }
}