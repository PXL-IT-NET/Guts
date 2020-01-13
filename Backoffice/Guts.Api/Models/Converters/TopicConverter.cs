using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Business.Dtos;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.TopicAggregate;

namespace Guts.Api.Models.Converters
{
    public class TopicConverter : ITopicConverter
    {
        public TopicSummaryModel ToTopicSummaryModel(Topic topic, IList<AssignmentResultDto> assignmentResults)
        {
            if (topic.Assignments == null)
            {
                throw new ArgumentException("Chapter should have assignments loaded", nameof(topic));
            }

            if (topic.Assignments.Any(ex => ex.Tests == null))
            {
                throw new ArgumentException("All assignments of the chapter should have their tests loaded", nameof(topic));
            }

            var model = new TopicSummaryModel
            {
                Id = topic.Id,
                Code = topic.Code,
                Description = topic.Description,
                AssignmentSummaries = new List<AssignmentSummaryModel>()
            };

            foreach (var assignment in topic.Assignments.OrderBy(a => a.Code))
            {
                var assignmentSummaryModel = CreateAssignmentSummaryModel(assignment, assignmentResults);
                model.AssignmentSummaries.Add(assignmentSummaryModel);
            }
            return model;
        }

        public TopicStatisticsModel ToTopicStatisticsModel(Topic topic, IList<AssignmentStatisticsDto> assignmentStatistics, string unit)
        {
            var model = new TopicStatisticsModel
            {
                Id = topic.Id,
                Code = topic.Code,
                AssignmentStatistics = new List<AssignmentStatisticsModel>()
            };

            foreach (var assignment in topic.Assignments.OrderBy(a => a.Code))
            {
                var assignmentStatisticsModel = CreateAssignmentStatisticsModel(assignment, assignmentStatistics, unit);
                model.AssignmentStatistics.Add(assignmentStatisticsModel);
            }

            return model;
        }

        private AssignmentSummaryModel CreateAssignmentSummaryModel(Assignment assignment,
            IList<AssignmentResultDto> assignmentResults)
        {
            var assignmentSummaryModel = new AssignmentSummaryModel
            {
                AssignmentId = assignment.Id,
                Code = assignment.Code,
                Description = assignment.Description,
                NumberOfTests = assignment.Tests.Count
            };

            EnsureAssignmentDescription(assignmentSummaryModel, assignment);

            var matchingResult = assignmentResults.FirstOrDefault(result => result.AssignmentId == assignment.Id);
            if (matchingResult != null)
            {
                assignmentSummaryModel.NumberOfPassedTests = matchingResult.TestResults.Count(result => result.Passed);
                assignmentSummaryModel.NumberOfFailedTests = matchingResult.TestResults.Count(result => !result.Passed);
            }
            return assignmentSummaryModel;
        }

        private AssignmentStatisticsModel CreateAssignmentStatisticsModel(
            Assignment assignment,
            IList<AssignmentStatisticsDto> assignmentStatisticsList, 
            string unit)
        {
            var model = new AssignmentStatisticsModel
            {
                AssignmentId = assignment.Id,
                Code = assignment.Code,
                Description = assignment.Description,
                TotalNumberOfUnits = 0,
                Unit = unit,
                TestPassageStatistics = new List<TestPassageStatisticModel>()
            };

            EnsureAssignmentDescription(model, assignment);

            var assignmentStatistics = assignmentStatisticsList.FirstOrDefault(result => result.AssignmentId == assignment.Id);
            if (assignmentStatistics != null)
            {
                model.TotalNumberOfUnits = assignmentStatistics.TestPassageStatistics.Sum(s => s.AmountOfUsers);
                foreach (var testPassageStatistic in assignmentStatistics.TestPassageStatistics)
                {
                    var testPassageStatisticModel = new TestPassageStatisticModel
                    {
                        AmountOfPassedTestsRange = $"{testPassageStatistic.AmountOfPassedTests} tests",
                        NumberOfUsers = testPassageStatistic.AmountOfUsers
                    };
                    model.TestPassageStatistics.Add(testPassageStatisticModel);
                }
            }

            return model;
        }

        private static void EnsureAssignmentDescription(AssignmentModel assignmentModel, Assignment assignment)
        {
            int.TryParse(assignment.Code, out int assignmentNumber);
            if (assignmentModel.Description == assignmentNumber.ToString())
            {
                if (assignmentNumber > 0)
                {
                    assignmentModel.Description = "Assignment " + assignmentNumber;
                }
                else
                {
                    assignmentModel.Description = assignment.Code;
                }
            }
        }
    }
}