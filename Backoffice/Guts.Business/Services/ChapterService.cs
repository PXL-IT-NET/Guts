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
    internal class ChapterService : IChapterService
    {
        private readonly IChapterRepository _chapterRepository;
        private readonly ITopicRepository _topicRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IPeriodRepository _periodRepository;
        private readonly IAssignmentService _assignmentService;

        public ChapterService(IChapterRepository chapterRepository,
            ITopicRepository topicRepository,
            ICourseRepository courseRepository,
            IPeriodRepository periodRepository,
            IAssignmentService assignmentService)
        {
            _chapterRepository = chapterRepository;
            _topicRepository = topicRepository;
            _courseRepository = courseRepository;
            _periodRepository = periodRepository;
            _assignmentService = assignmentService;
        }

        public async Task<Chapter> GetOrCreateChapterAsync(string courseCode, Code chapterCode, int? periodId = null)
        {
            Period period = await _periodRepository.GetPeriodAsync(periodId);

            Chapter chapter;
            try
            {
                chapter = await _chapterRepository.GetSingleAsync(courseCode, chapterCode, period.Id);
            }
            catch (DataNotFoundException)
            {
                var course = await _courseRepository.GetSingleAsync(courseCode);
                chapter = new Chapter
                {
                    Code = chapterCode,
                    CourseId = course.Id,
                    PeriodId = period.Id
                };
                chapter = await _chapterRepository.AddAsync(chapter);
            }

            return chapter;
        }

        public async Task UpdateChapterAsync(int courseId, Code chapterCode, string description)
        {
            Period period = await _periodRepository.GetPeriodAsync(null);

            await _topicRepository.UpdateAsync(courseId, chapterCode, period.Id, description);
        }

        public async Task<Chapter> LoadChapterAsync(int courseId, Code chapterCode, int? periodId = null)
        {
            Period period = await _periodRepository.GetPeriodAsync(periodId);

            var chapter = await _chapterRepository.LoadWithAssignmentsAsync(courseId, chapterCode, period.Id);

            return chapter;
        }

        public async Task<Chapter> LoadChapterWithTestsAsync(int courseId, Code chapterCode, int? periodId = null)
        {
            Period period = await _periodRepository.GetPeriodAsync(periodId);
            Chapter chapter = await _chapterRepository.LoadWithAssignmentsAndTestsAsync(courseId, chapterCode, period.Id);

            return chapter;
        }

        public async Task<IReadOnlyList<AssignmentResultDto>> GetResultsForUserAsync(Chapter chapter, int userId, DateTime? dateUtc)
        {
            var results = new List<AssignmentResultDto>();
            foreach (var assignment in chapter.Assignments)
            {
                var dto = await _assignmentService.GetResultsForUserAsync(assignment.Id, userId, dateUtc);
                results.Add(dto);
            }

            return results;
        }

        public async Task<IReadOnlyList<AssignmentStatisticsDto>> GetChapterStatisticsAsync(Chapter chapter, DateTime? dateUtc)
        {
            var results = new List<AssignmentStatisticsDto>();
            foreach (var assignment in chapter.Assignments)
            {
                var assignmentStatisticsDto = await _assignmentService.GetAssignmentUserStatisticsAsync(assignment.Id, dateUtc);
                results.Add(assignmentStatisticsDto);
            }
            return results;
        }

        public async Task<IReadOnlyList<Chapter>> GetChaptersOfCourseAsync(int courseId, int? periodId = null)
        {
            Period period;
            try
            {
                period = await _periodRepository.GetPeriodAsync(periodId);
            }
            catch (DataNotFoundException)
            {
                return new List<Chapter>();
            }

            IList<Chapter> chapters = await _chapterRepository.GetByCourseIdAsync(courseId, period.Id);
            return chapters.ToList();
        }
    }
}