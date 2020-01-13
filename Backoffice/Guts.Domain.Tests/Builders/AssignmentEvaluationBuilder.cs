﻿using Guts.Common.Extensions;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.ExamAggregate;

namespace Guts.Domain.Tests.Builders
{
    internal class AssignmentEvaluationBuilder : BaseBuilder<AssignmentEvaluation>
    {
        public AssignmentEvaluationBuilder()
        {
            ConstructItem();
            Item.Id = 0;
            SetProperty(ae => ae.AssignmentId, Random.NextPositive());
            SetProperty(ae => ae.ExamPartId, Random.NextPositive());
            SetProperty(ae => ae.MaximumScore, Random.NextPositive());
            SetProperty(ae => ae.NumberOfTestsAlreadyGreenAtStart, Random.Next(0, 6));
        }

        public AssignmentEvaluationBuilder WithId()
        {
            SetProperty(ae => ae.Id, Random.NextPositive());
            return this;
        }

        public AssignmentEvaluationBuilder WithAssignment(Assignment assignment)
        {
            SetProperty(ae => ae.Assignment, assignment);
            SetProperty(ae => ae.AssignmentId, assignment.Id);
            return this;
        }

        public AssignmentEvaluationBuilder WithMaximumScore(int maximumScore)
        {
            SetProperty(ae => ae.MaximumScore, maximumScore);
            return this;
        }

        public AssignmentEvaluationBuilder WithNumberOfTestsAlreadyGreenAtStart(int numberOfTestsAlreadyGreen)
        {
            SetProperty(ae => ae.NumberOfTestsAlreadyGreenAtStart, numberOfTestsAlreadyGreen);
            return this;
        }
    }
}