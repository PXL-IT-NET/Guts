namespace Guts.Domain.ExamAggregate
{
    public interface IExamFactory
    {
        Exam CreateNew(int courseId, string name);
    }
}