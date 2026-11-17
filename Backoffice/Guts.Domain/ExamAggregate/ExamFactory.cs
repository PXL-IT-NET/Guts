using System;

namespace Guts.Domain.ExamAggregate;

public class ExamFactory : IExamFactory
{
    public Exam CreateNew(
        int courseId,
        int periodId,
        int createdByUserId,
        ExamMetadata metadata,
        DateTime createdAtUtc)
    {
        return new Exam(
            courseId,
            periodId,
            createdByUserId,
            metadata,
            createdAtUtc);
    }
}
