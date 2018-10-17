using System;
using System.Collections.Generic;
using Guts.Api.Models;
using Guts.Common.Extensions;
using Guts.Domain;

namespace Guts.Api.Tests.Builders
{
    internal abstract class CreateTestRunModelBuilderBase<TModel> where TModel : CreateTestRunModelBase
    {
        protected readonly Random Random;

        protected TModel Model;

        protected CreateTestRunModelBuilderBase()
        {
            Random = new Random();
        }

        public CreateTestRunModelBuilderBase<TModel> WithSourceCode()
        {
            Model.SourceCode = Guid.NewGuid().ToString();

            return this;
        }

        public CreateTestRunModelBuilderBase<TModel> WithRandomTestResultModels(int numberOfTestResults)
        {
            var testResultModels = new List<TestResultModel>();
            for (int i = 0; i < numberOfTestResults; i++)
            {
                var testResultModel = new TestResultModel()
                {
                    TestName = Guid.NewGuid().ToString(),
                    Passed = Random.NextBool(),
                    Message = Guid.NewGuid().ToString()
                };
                testResultModels.Add(testResultModel);
            }
            Model.Results = testResultModels;
            return this;
        }

        public CreateTestRunModelBuilderBase<TModel> WithTestResultModels(IEnumerable<TestResultModel> testResultModels)
        {
            Model.Results = testResultModels;
            return this;
        }

        public CreateTestRunModelBuilderBase<TModel> WithRandomTestResultModelsFor(IEnumerable<Test> tests)
        {
            var results = new List<TestResultModel>();
            foreach (var test in tests)
            {
                var testResultModel = new TestResultModel()
                {
                    TestName = test.TestName,
                    Passed = Random.NextBool(),
                    Message = Guid.NewGuid().ToString()
                };
                results.Add(testResultModel);
            }
            Model.Results = results;

            return this;
        }

        public TModel Build()
        {
            return Model;
        }
    }
}