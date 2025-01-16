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
        Task<Chapter> GetOrCreateChapterAsync(string courseCode, Code chapterCode, int? periodId = null);
        Task UpdateChapterAsync(int courseId, Code chapterCode, string description);
        Task<Chapter> LoadChapterAsync(int courseId, Code chapterCode, int? periodId = null);
        Task<Chapter> LoadChapterWithTestsAsync(int courseId, Code chapterCode, int? periodId = null);
        Task<IReadOnlyList<AssignmentResultDto>> GetResultsForUserAsync(Chapter chapter, int userId, DateTime? dateUtc);
        Task<IReadOnlyList<AssignmentStatisticsDto>> GetChapterStatisticsAsync(Chapter chapter, DateTime? dateUtc);
        Task<IReadOnlyList<Chapter>> GetChaptersOfCourseAsync(int courseId, int? periodId = null);
    }
}