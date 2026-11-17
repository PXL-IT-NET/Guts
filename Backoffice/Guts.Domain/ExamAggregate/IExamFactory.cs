using System;

namespace Guts.Domain.ExamAggregate;

public interface IExamFactory
{
    Exam CreateNew(
        int courseId,
        int periodId,
        int createdByUserId,
        ExamMetadata metadata,
        DateTime createdAtUtc);
}
