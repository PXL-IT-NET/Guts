using System;
using System.Collections.Generic;
using Guts.Api.Models;
using Guts.Business.Dtos;
using Guts.Business.Tests.Builders;
using Guts.Common.Extensions;
using Guts.Domain.TestAggregate;

namespace Guts.Api.Tests.Builders
{
    internal class CreateAssignmentTestRunModelBuilder
    {
        private readonly Random _random;
        private readonly CreateAssignmentTestRunModel _model;

        public CreateAssignmentTestRunModelBuilder()
        {
            _random = new Random();
            _model = new CreateAssignmentTestRunModel
            {
                Assignment = new AssignmentDtoBuilder().Build(),
                Results = new List<TestResultModel>(),
                SourceCode = null,
                TestCodeHash = null
            };
        }

        public CreateAssignmentTestRunModelBuilder WithAssignment(AssignmentDto assignmentDto)
        {
            _model.Assignment = assignmentDto;

            return this;
        }

        public CreateAssignmentTestRunModelBuilder WithSourceCode()
        {
            _model.SourceCode = Guid.NewGuid().ToString();
            return this;
        }

        public CreateAssignmentTestRunModelBuilder WithTestCodeHash()
        {
            _model.TestCodeHash = Guid.NewGuid().ToString();
            return this;
        }

        public CreateAssignmentTestRunModelBuilder WithRandomTestResultModels(int numberOfTestResults)
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

        public CreateAssignmentTestRunModelBuilder WithTestResultModels(IEnumerable<TestResultModel> testResultModels)
        {
            _model.Results = testResultModels;
            return this;
        }

        public CreateAssignmentTestRunModelBuilder WithRandomTestResultModelsFor(IEnumerable<Test> tests)
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

        public CreateAssignmentTestRunModel Build()
        {
            return _model;
        }
    }
}