using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Business.Dtos;
using Guts.Business.Repositories;
using Guts.Domain.PeriodAggregate;
using Guts.Domain.TopicAggregate.ChapterAggregate;
using Guts.Domain.ValueObjects;

namespace Guts.Business.Services
{
    internal class ChapterService(
        IChapterRepository chapterRepository,
        ITopicRepository topicRepository,
        ICourseRepository courseRepository,
        IPeriodRepository periodRepository,
        IAssignmentService assignmentService)
        : IChapterService
    {
        public async Task<Chapter> GetOrCreateChapterAsync(string courseCode, Code chapterCode, int? periodId = null)
        {
            IPeriod period = await periodRepository.GetPeriodAsync(periodId);

            Chapter chapter;
            try
            {
                chapter = await chapterRepository.GetSingleAsync(courseCode, chapterCode, period.Id);
            }
            catch (DataNotFoundException)
            {
                var course = await courseRepository.GetSingleAsync(courseCode);
                chapter = new Chapter
                {
                    Code = chapterCode,
                    CourseId = course.Id,
                    PeriodId = period.Id
                };
                chapter = await chapterRepository.AddAsync(chapter);
            }

            return chapter;
        }

        public async Task UpdateChapterAsync(int courseId, Code chapterCode, string description)
        {
            IPeriod period = await periodRepository.GetPeriodAsync(null);

            await topicRepository.UpdateAsync(courseId, chapterCode, period.Id, description);
        }

        public async Task<Chapter> LoadChapterAsync(int courseId, Code chapterCode, int? periodId = null)
        {
            IPeriod period = await periodRepository.GetPeriodAsync(periodId);

            var chapter = await chapterRepository.LoadWithAssignmentsAsync(courseId, chapterCode, period.Id);

            return chapter;
        }

        public async Task<Chapter> LoadChapterWithTestsAsync(int courseId, Code chapterCode, int? periodId = null)
        {
            IPeriod period = await periodRepository.GetPeriodAsync(periodId);
            Chapter chapter = await chapterRepository.LoadWithAssignmentsAndTestsAsync(courseId, chapterCode, period.Id);

            return chapter;
        }

        public async Task<IReadOnlyList<AssignmentResultDto>> GetResultsForUserAsync(Chapter chapter, int userId, DateTime? dateUtc)
        {
            var results = new List<AssignmentResultDto>();
            foreach (var assignment in chapter.Assignments)
            {
                var dto = await assignmentService.GetResultsForUserAsync(assignment.Id, userId, dateUtc);
                results.Add(dto);
            }

            return results;
        }

        public async Task<IReadOnlyList<AssignmentStatisticsDto>> GetChapterStatisticsAsync(Chapter chapter, DateTime? dateUtc)
        {
            var results = new List<AssignmentStatisticsDto>();
            foreach (var assignment in chapter.Assignments)
            {
                var assignmentStatisticsDto = await assignmentService.GetAssignmentUserStatisticsAsync(assignment.Id, dateUtc);
                results.Add(assignmentStatisticsDto);
            }
            return results;
        }

        public async Task<IReadOnlyList<Chapter>> GetChaptersOfCourseAsync(int courseId, int? periodId = null)
        {
            IPeriod period;
            try
            {
                period = await periodRepository.GetPeriodAsync(periodId);
            }
            catch (DataNotFoundException)
            {
                return new List<Chapter>();
            }

            IList<Chapter> chapters = await chapterRepository.GetByCourseIdAsync(courseId, period.Id);
            return chapters.ToList();
        }
    }
}