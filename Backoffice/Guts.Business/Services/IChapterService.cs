using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Business.Dtos;
using Guts.Domain.TopicAggregate.ChapterAggregate;
using Guts.Domain.ValueObjects;

namespace Guts.Business.Services
{
    public interface IChapterService
    {
        Task<Chapter> GetOrCreateChapterAsync(string courseCode, Code chapterCode);
        Task UpdateChapterAsync(int courseId, Code chapterCode, int? periodId, string description);
        Task<Chapter> LoadChapterAsync(int courseId, Code chapterCode);
        Task<Chapter> LoadChapterWithTestsAsync(int courseId, Code chapterCode);
        Task<IReadOnlyList<AssignmentResultDto>> GetResultsForUserAsync(Chapter chapter, int userId, DateTime? dateUtc);
        Task<IReadOnlyList<AssignmentStatisticsDto>> GetChapterStatisticsAsync(Chapter chapter, DateTime? dateUtc);
        Task<IReadOnlyList<Chapter>> GetChaptersOfCourseAsync(int courseId);
    }
}