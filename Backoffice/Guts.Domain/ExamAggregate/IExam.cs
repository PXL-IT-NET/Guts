using System;
using System.Collections.Generic;
using Guts.Domain.CourseAggregate;
using Guts.Domain.PeriodAggregate;
using Guts.Domain.UserAggregate;

namespace Guts.Domain.ExamAggregate
{
    public interface IExam : IEntity
    {
        Course Course { get; }
        int CourseId { get; }
        Period Period { get; }
        int PeriodId { get; set; }
        string Name { get; }
        int MaximumScore { get; } //TODO: rename to NormalizedMaximumScore
        IReadOnlyCollection<IExamPart> Parts { get; }
        ExamPart AddExamPart(string name, DateTime deadline);
        void DeleteExamPart(int examPartId);
        IExamScore CalculateScoreForUser(User user, IExamTestResultCollection examTestResults);
    }
}