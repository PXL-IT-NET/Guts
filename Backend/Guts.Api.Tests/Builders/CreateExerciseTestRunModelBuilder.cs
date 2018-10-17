using System.Collections.Generic;
using Guts.Api.Models;
using Guts.Business;
using Guts.Business.Tests.Builders;

namespace Guts.Api.Tests.Builders
{
    internal class CreateExerciseTestRunModelBuilder : CreateTestRunModelBuilderBase<CreateExerciseTestRunModel>
    {
        public CreateExerciseTestRunModelBuilder()
        {
            Model = new CreateExerciseTestRunModel
            {
                Exercise = new ExerciseDtoBuilder().Build(),
                Results = new List<TestResultModel>(),
                SourceCode = null
            };
        }

        public CreateExerciseTestRunModelBuilder WithExercise(ExerciseDto exerciseDto)
        {
            Model.Exercise = exerciseDto;

            return this;
        }
    }
}