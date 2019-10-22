using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Business.Dtos;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.TestRunAggregate;

namespace Guts.Api.Models.Converters
{
    public class AssignmentConverter : IAssignmentConverter
    {
        public AssignmentDetailModel ToAssignmentDetailModel(Assignment assignment, IList<TestResult> results, AssignmentTestRunInfoDto testRunInfo)
        {
            if (assignment.Topic?.Course == null)
            {
                throw new ArgumentException("Assignment should have topic and course loaded", nameof(assignment));
            }

            if (assignment.Tests == null)
            {
                throw new ArgumentException("Assignment should have tests loaded", nameof(assignment));
            }

            if (testRunInfo == null)
            {
                throw new ArgumentNullException(nameof(testRunInfo));
            }

            var model = new AssignmentDetailModel
            {
                TopicCode = assignment.Topic.Code,
                Code = assignment.Code,
                AssignmentId = assignment.Id,
                CourseName = assignment.Topic.Course.Name,
                CourseId= assignment.Topic.CourseId,
                TestResults = new List<TestResultModel>(),
                FirstRun =  testRunInfo.FirstRunDateTime, 
                LastRun = testRunInfo.LastRunDateTime,
                NumberOfRuns = testRunInfo.NumberOfRuns,
                SourceCode = testRunInfo.SourceCode
            };

            foreach (var test in assignment.Tests)
            {
                var testResultModel = new TestResultModel
                {
                    TestName = test.TestName,
                    Runned = false,
                    Passed = false,
                    Message = string.Empty
                };

                var matchingResult = results?.FirstOrDefault(r => r.TestId == test.Id);
                if (matchingResult != null)
                {
                    testResultModel.Runned = true;
                    testResultModel.Passed = matchingResult.Passed;
                    testResultModel.Message = matchingResult.Message;
                }

                model.TestResults.Add(testResultModel);
            }

            return model;
        }
    }
}