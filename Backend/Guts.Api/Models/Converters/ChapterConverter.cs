using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Business;
using Guts.Domain;

namespace Guts.Api.Models.Converters
{
    public class ChapterConverter : IChapterConverter
    {
        private readonly IUserConverter _userConverter;

        public ChapterConverter(IUserConverter userConverter)
        {
            _userConverter = userConverter;
        }

        public ChapterSummaryModel ToChapterSummaryModel(Chapter chapter, IList<AssignmentResultDto> userAssignmentResults)
        {
            if (chapter.Assignments == null)
            {
                throw new ArgumentException("Chapter should have assignments loaded", nameof(chapter));
            }

            if (chapter.Assignments.Any(ex => ex.Tests == null))
            {
                throw new ArgumentException("All assignments of the chapter should have their tests loaded", nameof(chapter));
            }

            var model = new ChapterSummaryModel
            {
                Id = chapter.Id,
                Code = chapter.Code,
                Description = chapter.Description,
                ExerciseSummaries = new List<AssignmentSummaryModel>()
            };

            foreach (var assignment in chapter.Assignments.OrderBy(a => a.Code))
            {
                var assignmentSummaryModel = CreateAssignmentSummaryModel(assignment, userAssignmentResults);
                model.ExerciseSummaries.Add(assignmentSummaryModel);
            }
            return model;
        }

        public ChapterModel ToChapterModel(Chapter chapter)
        {
            return new ChapterModel
            {
                Id = chapter.Id,
                Code = chapter.Code,
                Description = chapter.Description
            };
        }

        public ChapterDetailModel ToChapterDetailModel(Chapter chapter, IList<User> chapterUsers)
        {
            return new ChapterDetailModel
            {
                Id = chapter.Id,
                Code = chapter.Code,
                Exercises = chapter.Assignments.Select(assignment => new AssignmentModel
                {
                    AssignmentId = assignment.Id,
                    Code = assignment.Code
                }).ToList(),
                Users = chapterUsers.Select(user => _userConverter.FromUser(user)).ToList()
            };
        }

        public ChapterStatisticsModel ToChapterStatisticsModel(Chapter chapter, IList<AssignmentStatisticsDto> chapterStatistics)
        {
            var model = new ChapterStatisticsModel
            {
                Id = chapter.Id,
                Code = chapter.Code,
                ExerciseStatistics = new List<AssignmentStatisticsModel>()
            };

            foreach (var assignment in chapter.Assignments.OrderBy(a => a.Code))
            {
                var assignmentStatisticsModel = CreateAssignmentStatisticsModel(assignment, chapterStatistics);
                model.ExerciseStatistics.Add(assignmentStatisticsModel);
            }

            return model;
        }

        private AssignmentSummaryModel CreateAssignmentSummaryModel(Assignment assigment,
            IList<AssignmentResultDto> assignmentResults)
        {
            var assignmentSummaryModel = new AssignmentSummaryModel
            {
                AssignmentId = assigment.Id,
                Code = assigment.Code,
                NumberOfTests = assigment.Tests.Count
            };

            var matchingResult = assignmentResults.FirstOrDefault(result => result.AssignmentId == assigment.Id);
            if (matchingResult != null)
            {
                assignmentSummaryModel.NumberOfPassedTests = matchingResult.TestResults.Count(result => result.Passed);
                assignmentSummaryModel.NumberOfFailedTests = matchingResult.TestResults.Count(result => !result.Passed);
            }
            return assignmentSummaryModel;
        }

        private AssignmentStatisticsModel CreateAssignmentStatisticsModel(Assignment assignment,
            IList<AssignmentStatisticsDto> chapterStatistics)
        {
            var model = new AssignmentStatisticsModel
            {
                AssignmentId = assignment.Id,
                Code = assignment.Code,
                TotalNumberOfUsers = 0,
                TestPassageStatistics = new List<TestPassageStatisticModel>()
            };

            var assignmentStatistics = chapterStatistics.FirstOrDefault(result => result.AssignmentId == assignment.Id);
            if (assignmentStatistics != null)
            {
                model.TotalNumberOfUsers = assignmentStatistics.TestPassageStatistics.Sum(s => s.AmountOfUsers);
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
    }
}
