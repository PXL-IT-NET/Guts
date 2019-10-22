using System.Collections.Generic;
using Guts.Domain.ExamAggregate;

namespace Guts.Api.Models.Converters
{
    public class EvaluationConverter : IEvaluationConverter
    {
        public ExamPartOutputModel ToEvaluationOutputModel(ExamPart examPart)
        {
            var model = new ExamPartOutputModel
            {
                Id = examPart.Id,
                Name = examPart.Name,
                Deadline = examPart.Deadline,
                AssignmentEvaluations = new List<AssignmentEvaluationOutputModel>()
            };

            foreach (var assignmentEvaluation in examPart.AssignmentEvaluations)
            {
                model.AssignmentEvaluations.Add(new AssignmentEvaluationOutputModel
                {
                    Id = assignmentEvaluation.Id,
                    AssignmentId = assignmentEvaluation.AssignmentId,
                    MaximumScore = assignmentEvaluation.MaximumScore,
                    NumberOfTestsAlreadyGreenAtStart = assignmentEvaluation.NumberOfTestsAlreadyGreenAtStart
                });
            }

            return model;
        }
    }
}