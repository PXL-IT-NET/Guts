using System;
using System.Collections.Generic;
using Guts.Api.Models;
using Guts.Business;
using Guts.Business.Tests.Builders;
using Guts.Common.Extensions;
using Guts.Domain;

namespace Guts.Api.Tests.Builders
{
    internal class ExerciseCreateTestRunModelBuilder
    {
        private readonly Random _random;
        private readonly ExerciseCreateTestRunModel _model;

        public ExerciseCreateTestRunModelBuilder()
        {
            _random = new Random();
            _model = new ExerciseCreateTestRunModel
            {
                Exercise = new ExerciseDtoBuilder().Build(),
                Results = new List<TestResultModel>(),
                SourceCode = null
            };
        }

        public ExerciseCreateTestRunModelBuilder WithExercise(ExerciseDto exerciseDto)
        {
            _model.Exercise = exerciseDto;

            return this;
        }

        public ExerciseCreateTestRunModelBuilder WithSourceCode()
        {
            _model.SourceCode = Guid.NewGuid().ToString();

            return this;
        }

        public ExerciseCreateTestRunModelBuilder WithRandomTestResultModels(int numberOfTestResults)
        {
            var testResultModels = new List<TestResultModel>();
            for (int i = 0; i < numberOfTestResults; i++)
            {
                var testResultModel = new TestResultModel()
                {
                    TestName = Guid.NewGuid().ToString(),
                    Passed = _random.NextBool(),
                    Message = Guid.NewGuid().ToString()
                };
                testResultModels.Add(testResultModel);
            }
            _model.Results = testResultModels;
            return this;
        }

        public ExerciseCreateTestRunModelBuilder WithTestResultModels(IEnumerable<TestResultModel> testResultModels)
        {
            _model.Results = testResultModels;
            return this;
        }

        public ExerciseCreateTestRunModelBuilder WithRandomTestResultModelsFor(IEnumerable<Test> tests)
        {
            var results = new List<TestResultModel>();
            foreach (var test in tests)
            {
                var testResultModel = new TestResultModel()
                {
                    TestName = test.TestName,
                    Passed = _random.NextBool(),
                    Message = Guid.NewGuid().ToString()
                };
                results.Add(testResultModel);
            }
            _model.Results = results;

            return this;
        }

        public ExerciseCreateTestRunModel Build()
        {
            return _model;
        }

    }
}