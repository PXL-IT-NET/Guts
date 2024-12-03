using System;
using System.Collections.Generic;
using Guts.Common.Extensions;
using Guts.Domain.ExamAggregate;

namespace Guts.Domain.Tests.Builders
{
    internal class ExamPartBuilder : BaseBuilder<ExamPart>
    {
        public ExamPartBuilder()
        {
            int examId = 0;
            string name = Random.Shared.NextString();
            var deadline = DateTime.UtcNow.AddDays(Random.Shared.Next(1, 101));

            ConstructItem(examId, name, deadline);
        }

        public ExamPartBuilder WithId()
        {
            SetProperty(part => part.Id, Random.Shared.NextPositive());
            return this;
        }

        public ExamPartBuilder WithExamId()
        {
            SetProperty(part => part.ExamId, Random.Shared.NextPositive());
            return this;
        }

        public ExamPartBuilder WithExamId(int examId)
        {
            SetProperty(part => part.ExamId, examId);
            return this;
        }

        public ExamPartBuilder WithAssignmentEvaluation(IAssignmentEvaluation assignmentEvaluation)
        {
            var evaluations = GetFieldValue<HashSet<IAssignmentEvaluation>>();
            evaluations.Add(assignmentEvaluation);
            return this;
        }
    }
}