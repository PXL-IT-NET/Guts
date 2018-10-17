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
                ExerciseCode = Guid.NewGuid().ToString()
            };
        }

        public ExerciseDtoBuilder WithExerciseCode(string code)
        {
            _dto.ExerciseCode = code;
            return this;
        }

        public ExerciseDto Build()
        {
            return _dto;
        }
    }
}