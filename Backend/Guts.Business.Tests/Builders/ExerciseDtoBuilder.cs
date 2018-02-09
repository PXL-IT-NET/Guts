using System;
using Guts.Common.Extensions;

namespace Guts.Business.Tests.Builders
{
    public class ExerciseDtoBuilder
    {
        private readonly Random _random;
        private readonly ExerciseDto _dto;

        public ExerciseDtoBuilder()
        {
            _random = new Random();
            _dto = new ExerciseDto
            {
                CourseCode = Guid.NewGuid().ToString(),
                ChapterNumber = RandomExtensions.NextPositive(_random),
                ExerciseNumber = RandomExtensions.NextPositive(_random),
            };
        }

        public ExerciseDtoBuilder WithNumber(int exerciseNumber)
        {
            _dto.ExerciseNumber = exerciseNumber;
            return this;
        }

        public ExerciseDto Build()
        {
            return _dto;
        }
    }
}