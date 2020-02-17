﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Business.Dtos;
using Guts.Business.Repositories;
using Guts.Domain.PeriodAggregate;
using Guts.Domain.TopicAggregate.ChapterAggregate;

namespace Guts.Business.Services
{
    internal class ChapterService : IChapterService
    {
        private readonly IChapterRepository _chapterRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IPeriodRepository _periodRepository;
        private readonly IAssignmentService _assignmentService;

        public ChapterService(IChapterRepository chapterRepository,
            ICourseRepository courseRepository,
            IPeriodRepository periodRepository,
            IAssignmentService assignmentService)
        {
            _chapterRepository = chapterRepository;
            _courseRepository = courseRepository;
            _periodRepository = periodRepository;
            _assignmentService = assignmentService;
        }

        public async Task<Chapter> GetOrCreateChapterAsync(string courseCode, string chapterCode)
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

        public async Task<Chapter> LoadChapterAsync(int courseId, string chapterCode)
        {
            var currentPeriod = await _periodRepository.GetCurrentPeriodAsync();

            var chapter = await _chapterRepository.LoadWithAssignmentsAsync(courseId, chapterCode, currentPeriod.Id);

            return chapter;
        }

        public async Task<Chapter> LoadChapterWithTestsAsync(int courseId, string chapterCode)
        {
            var currentPeriod = await _periodRepository.GetCurrentPeriodAsync();

            var chapter = await _chapterRepository.LoadWithAssignmentsAndTestsAsync(courseId, chapterCode, currentPeriod.Id);

            return chapter;
        }

        public async Task<IList<AssignmentResultDto>> GetResultsForUserAsync(Chapter chapter, int userId, DateTime? dateUtc)
        {
            var results = new List<AssignmentResultDto>();
            foreach (var assignment in chapter.Assignments)
            {
                var dto = await _assignmentService.GetResultsForUserAsync(assignment.Id, userId, dateUtc);
                results.Add(dto);
            }

            return results;
        }

        public async Task<IList<AssignmentStatisticsDto>> GetChapterStatisticsAsync(Chapter chapter, DateTime? dateUtc)
        {
            var results = new List<AssignmentStatisticsDto>();
            foreach (var assignment in chapter.Assignments)
            {
                var assignmentStatisticsDto = await _assignmentService.GetAssignmentUserStatisticsAsync(assignment.Id, dateUtc);
                results.Add(assignmentStatisticsDto);
            }
            return results;
        }

        public async Task<IList<Chapter>> GetChaptersOfCourseAsync(int courseId)
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

            var chapters = await _chapterRepository.GetByCourseIdAsync(courseId, currentPeriod.Id);
            return chapters.OrderBy(chapter => chapter.Code).ToList();
        }
    }
}