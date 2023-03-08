using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Business.Dtos;
using Guts.Domain.TopicAggregate.ChapterAggregate;

namespace Guts.Business.Services
{
    public interface IChapterService
    {
        Task<Chapter> GetOrCreateChapterAsync(string courseCode, string chapterCode);
        Task<Chapter> LoadChapterAsync(int courseId, string chapterCode);
        Task<Chapter> LoadChapterWithTestsAsync(int courseId, string chapterCode);
        Task<IReadOnlyList<AssignmentResultDto>> GetResultsForUserAsync(Chapter chapter, int userId, DateTime? dateUtc);
        Task<IReadOnlyList<AssignmentStatisticsDto>> GetChapterStatisticsAsync(Chapter chapter, DateTime? dateUtc);
        Task<IReadOnlyList<Chapter>> GetChaptersOfCourseAsync(int courseId);
    }
}