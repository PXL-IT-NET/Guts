using System;
using System.Collections.Generic;
using Guts.Api.Models;
using Guts.Business;
using Guts.Business.Tests.Builders;
using Guts.Common.Extensions;
using Guts.Domain;

namespace Guts.Api.Tests.Builders
{
    internal class CreateTestRunModelBuilder
    {
        private readonly Random _random;
        private readonly CreateTestRunModel _model;

        public CreateTestRunModelBuilder()
        {
            _random = new Random();
            _model = new CreateTestRunModel
            {
                Exercise = new ExerciseDtoBuilder().Build(),
                Results = new List<TestResultModel>(),
                SourceCode = null
            };
        }

        public CreateTestRunModelBuilder WithExercise(ExerciseDto exerciseDto)
        {
            _model.Exercise = exerciseDto;

            return this;
        }

        public CreateTestRunModelBuilder WithSourceCode()
        {
            _model.SourceCode = Guid.NewGuid().ToString();

            return this;
        }

        public CreateTestRunModelBuilder WithRandomTestResultModels(int numberOfTestResults)
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

        public CreateTestRunModelBuilder WithTestResultModels(IEnumerable<TestResultModel> testResultModels)
        {
            _model.Results = testResultModels;
            return this;
        }

        public CreateTestRunModelBuilder WithRandomTestResultModelsFor(IEnumerable<Test> tests)
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

        public CreateTestRunModel Build()
        {
            return _model;
        }

    }
}