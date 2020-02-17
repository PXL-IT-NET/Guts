using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Business.Dtos;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.TestRunAggregate;
using Guts.Domain.ValueObjects;

namespace Guts.Api.Models.Converters
{
    public class AssignmentConverter : IAssignmentConverter
    {
        public AssignmentDetailModel ToAssignmentDetailModel(
            Assignment assignment,
            AssignmentTestRunInfoDto testRunInfo,
            IList<TestResult> results,
            IList<SolutionFile> solutionFiles)
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

            results ??= new List<TestResult>();
            solutionFiles ??= new List<SolutionFile>();

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
                SolutionFiles = new List<SolutionFileModel>()
            };

            AddTestResults(model, assignment, results);

            AddSolutionFiles(model, solutionFiles);

            return model;
        }

        private void AddTestResults(AssignmentDetailModel model, Assignment assignment,
            IList<TestResult> results)
        {
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
        }

        private void AddSolutionFiles(AssignmentDetailModel model, IList<SolutionFile> solutionFiles)
        {
            foreach (var solutionFile in solutionFiles)
            {
                var solutionFileModel = new SolutionFileModel
                {
                    FilePath = solutionFile.FilePath,
                    Content = solutionFile.Content
                };
                model.SolutionFiles.Add(solutionFileModel);
            }
        }
    }
}