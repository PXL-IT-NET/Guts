namespace Guts.Domain.ExamAggregate
{
    public interface IExamTestResultCollection
    {
        void AddExamPartResults(int examPartId, IExamPartTestResultCollection examPartTestResults);
        IExamPartTestResultCollection GetExamPartResults(int examPartId);
    }
}