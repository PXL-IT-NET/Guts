using System;
using Guts.Common.Extensions;

namespace Guts.Business.Tests.Builders
{
    public class ExerciseDtoBuilder
    {
        private readonly ExerciseDto _dto;

        public ExerciseDtoBuilder()
        {
            var random = new Random();
            _dto = new ExerciseDto
            {
                CourseCode = Guid.NewGuid().ToString(),
                ChapterNumber = random.NextPositive(),
                ExerciseNumber = random.NextPositive()
            };
        }

        public ExerciseDtoBuilder WithExerciseNumber(int number)
        {
            _dto.ExerciseNumber = number;
            return this;
        }

        public ExerciseDto Build()
        {
            return _dto;
        }
    }
}