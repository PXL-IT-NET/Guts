using System;
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
                    var numberOfUsers = testWithLastUserResults.ResultsOfUsers.Count();
                    var numberOfUsersThatPassTheTest =
                        testWithLastUserResults.ResultsOfUsers.Count(result => result.Passed);
                    var passedOnAverage = (int)Math.Round(numberOfUsersThatPassTheTest / (double) numberOfUsers) == 1;
                    var mostOccuringMessage = (from userResult in testWithLastUserResults.ResultsOfUsers
                        group userResult by userResult.Message
                        into messageGroup
                        orderby messageGroup.Count() descending
                        select messageGroup.Key).FirstOrDefault();

                    var testResultDto = new TestResultDto
                    {
                        TestName = testWithLastUserResults.Test.TestName,
                        Passed = passedOnAverage,
                        Message = mostOccuringMessage
                    };
                    resultDto.TestResults.Add(testResultDto);
                }

                results.Add(resultDto);
            }
            return results;
        }
    }
}