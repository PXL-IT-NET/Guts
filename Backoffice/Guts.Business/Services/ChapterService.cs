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

        public async Task<Chapter> GetOrCreateChapterAsync(string courseCode, Code chapterCode)
        {
            var currentPeriod = await _periodRepository.GetCurrentPeriodAsync();

            Chapter chapter;
            try
            {
                chapter = await _chapterRepository.GetSingleAsync(courseCode, chapterCode, currentPeriod.Id);
            }
            catch (DataNotFoundException)
            {
                var course = await _courseRepository.GetSingleAsync(courseCode);
                chapter = new Chapter
                {
                    Code = chapterCode,
                    CourseId = course.Id,
                    PeriodId = currentPeriod.Id
                };
                chapter = await _chapterRepository.AddAsync(chapter);
            }

            return chapter;
        }

        public async Task UpdateChapterAsync(int courseId, Code chapterCode, int? periodId, string description)
        {
            if (!periodId.HasValue)
            {
                Period currentPeriod = await _periodRepository.GetCurrentPeriodAsync();
                periodId = currentPeriod.Id;
            }

            await _topicRepository.UpdateAsync(courseId, chapterCode, periodId.Value, description);
        }

        public async Task<Chapter> LoadChapterAsync(int courseId, Code chapterCode)
        {
            var currentPeriod = await _periodRepository.GetCurrentPeriodAsync();

            var chapter = await _chapterRepository.LoadWithAssignmentsAsync(courseId, chapterCode, currentPeriod.Id);

            return chapter;
        }

        public async Task<Chapter> LoadChapterWithTestsAsync(int courseId, Code chapterCode)
        {
            var currentPeriod = await _periodRepository.GetCurrentPeriodAsync();

            var chapter = await _chapterRepository.LoadWithAssignmentsAndTestsAsync(courseId, chapterCode, currentPeriod.Id);

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

        public async Task<IReadOnlyList<Chapter>> GetChaptersOfCourseAsync(int courseId)
        {
            Period currentPeriod;
            try
            {
                currentPeriod = await _periodRepository.GetCurrentPeriodAsync();
            }
            catch (DataNotFoundException)
            {
                return new List<Chapter>();
            }

            IList<Chapter> chapters = await _chapterRepository.GetByCourseIdAsync(courseId, currentPeriod.Id);
            return chapters.ToList();
        }
    }
}