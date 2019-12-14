using Guts.Common.Extensions;
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
            SetProperty(part => part.Id, Random.NextPositive());
            return this;
        }

    }
}