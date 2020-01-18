using System;
using Guts.Business.Dtos;
using Guts.Common.Extensions;

namespace Guts.Business.Tests.Builders
{
    public class AssignmentEvaluationDtoBuilder
    {
        private readonly AssignmentEvaluationDto _dto;
        private Random _random;

        public AssignmentEvaluationDtoBuilder()
        {
            _random = new Random();
            _dto = new AssignmentEvaluationDto
            {
                AssignmentId = _random.NextPositive(),
                MaximumScore = _random.Next(5, 101),
                NumberOfTestsAlreadyGreenAtStart = _random.Next(0, 5)
            };
        }

        public AssignmentEvaluationDto Build()
        {
            return _dto;
        }
    }
}