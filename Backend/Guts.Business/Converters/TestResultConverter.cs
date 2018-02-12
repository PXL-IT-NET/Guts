using System.Collections.Generic;
using System.Linq;
using Guts.Data;

namespace Guts.Business.Converters
{
    public class TestResultConverter : ITestResultConverter
    {
        public IList<ExerciseResultDto> ToExerciseResultDto(IList<TestWithLastUserResults> testsWithLastUserResults)
        {
            var results = new List<ExerciseResultDto>();

            var exerciseGroups = from testWithLastUserResults in testsWithLastUserResults
                group testWithLastUserResults by testWithLastUserResults.Test.ExerciseId;

            foreach (var exerciseGroup in exerciseGroups)
            {
                var resultDto = new ExerciseResultDto
                {
                    ExerciseId = exerciseGroup.Key,
                    TestResults = new List<TestResultDto>()
                };

                foreach (var testWithLastUserResults in exerciseGroup)
                {
                    foreach (var testResult in testWithLastUserResults.ResultsOfUsers)
                    {
                        var testResultDto = new TestResultDto
                        {
                            TestName = testWithLastUserResults.Test.TestName,
                            Passed = testResult.Passed,
                            Message = testResult.Message
                        };
                        resultDto.TestResults.Add(testResultDto);
                    }
                }

                results.Add(resultDto);
            }
            return results;
        }
    }
}