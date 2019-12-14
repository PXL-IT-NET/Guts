using System.Collections.Generic;
using Guts.Common.Extensions;
using Guts.Domain.ExamAggregate;

namespace Guts.Domain.Tests.Builders
{
    internal class ExamBuilder : BaseBuilder<Exam>
    {
        public ExamBuilder()
        {
            int courseId = Random.NextPositive();
            int periodId = Random.NextPositive();
            string name = Random.NextString();

            ConstructItem(courseId, periodId, name);
        }

        public ExamBuilder WithId(int examId)
        {
            SetProperty(exam => exam.Id, examId);
            return this;
        }

        public ExamBuilder WithExamPart(ExamPart examPart)
        {
            var examParts = GetFieldValue<HashSet<ExamPart>>();
            examParts.Add(examPart);
            return this;
        }
    }
}